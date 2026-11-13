# رفع بيانات الطور المحلي إلى قرص Fly الدائم (/data) عبر sftp put المباشر.
#
# تنبيه أمان (مهم): الماكينة الحية الآن طورٌ محلي يقرأ /data/zu_auditintel.db و/data/evidence.
# الكتابة فوق قاعدة مفتوحة في عملية حية قد تُفسدها. شغّل هذا السكريبت في نافذة ما قبل النشر
# (حين تكون الصورة السحابية القديمة هي المنشورة ولا تقرأ /data)، أو والماكينة متوقفة،
# ثم أعد النشر/التشغيل بعدها. لا تشغّله فوق موقع حي يخدم المستخدمين دون إيقافه أولاً.
#
# معالجة OneDrive: كل الجرد والنسخ يجري في Python (يرى ملفات OneDrive الوهمية ويُرطّبها)،
# فينسخ القاعدة نسخةً متسقة عبر backup API ويجمع الأدلة إلى مجلد مؤقت خارج OneDrive،
# ثم يرفع PowerShell من ذلك المجلد. هكذا لا تحجب أدوات .NET/PowerShell ملفات App_Data
# غير المرطّبة (dehydrated) التي يراها Python عادياً.
#
# النقل: fly ssh sftp put <محلي> <بعيد> --app <app> لكل ملف على حدة (لا sftp shell عبر stdin،
# فهو غير موثوق). المجلدات البعيدة تُنشأ أولاً بـ mkdir -p، والقاعدة تُنسخ نسخةً متسقة
# فلا تُرفع ملفات -wal/-shm معها. التحقق النهائي: بصمة القاعدة وعدد ملفات الأدلة على القرص
# مقابل البيان المحلي، فلا يُنشر فوق رفعٍ ناقص.
#
# من جذر المستودع:
#   powershell -ExecutionPolicy Bypass -File deploy/upload-data.ps1
# ثم deploy/fly.ps1.

param(
    [string]$App = "zu-auditintel",
    [string]$RepoRoot = (Split-Path $PSScriptRoot -Parent),
    [string]$DataRoot = ""
)

$ErrorActionPreference = "Stop"

# --- إيجاد flyctl ---
$flyExe = $null
foreach ($name in @("flyctl", "fly")) {
    $command = Get-Command $name -ErrorAction SilentlyContinue
    if ($command) { $flyExe = $command.Source; break }
}
if (-not $flyExe) {
    $candidate = Join-Path $env:USERPROFILE ".fly\bin\flyctl.exe"
    if (Test-Path -LiteralPath $candidate) { $flyExe = $candidate }
}
if (-not $flyExe) { throw "flyctl غير منصوب." }

$RepoRoot = (Resolve-Path -LiteralPath $RepoRoot).Path
if (-not $DataRoot) { $DataRoot = Join-Path $RepoRoot "ZU_AuditIntel.Web\AppData" }

# --- مجلد تجهيز خارج OneDrive ---
$staging = Join-Path ([System.IO.Path]::GetTempPath()) ("zu-upload-" + [guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $staging | Out-Null
try {
    # منطق التجهيز في Python: يرى OneDrive، ينسخ القاعدة نسخةً متسقة، ويكتب بياناً بالبصمات.
    # يُكتب بلا BOM حتى يقرؤه py -3 سليماً، ونصُّ Python حرفيٌّ هنا (here-string مفرد الاقتباس).
    $stagePy = Join-Path $staging "stage.py"
    $pySource = @'
import sqlite3, os, sys, json, shutil, hashlib

data_root, staging, manifest = sys.argv[1], sys.argv[2], sys.argv[3]
db = os.path.join(data_root, "zu_auditintel.db")
ev = os.path.join(data_root, "evidence")
if not os.path.exists(db):
    sys.exit("لا قاعدة في " + db)
if not os.path.isdir(ev):
    sys.exit("لا شجرة أدلة في " + ev)

os.makedirs(staging, exist_ok=True)
staged_db = os.path.join(staging, "zu_auditintel.db")
a = sqlite3.connect(db)
b = sqlite3.connect(staged_db)
a.backup(b)
b.close()
a.close()


def sha(p):
    h = hashlib.sha256()
    with open(p, "rb") as f:
        for chunk in iter(lambda: f.read(1 << 20), b""):
            h.update(chunk)
    return h.hexdigest()


files = []
for root, _, names in os.walk(ev):
    for n in sorted(names):
        full = os.path.join(root, n)
        rel = os.path.relpath(full, ev).replace("\\", "/")
        dst = os.path.join(staging, "evidence", rel.replace("/", os.sep))
        os.makedirs(os.path.dirname(dst), exist_ok=True)
        shutil.copy2(full, dst)
        files.append({"rel": rel, "sha256": sha(dst), "size": os.path.getsize(dst)})

m = {"db_sha256": sha(staged_db), "db_size": os.path.getsize(staged_db), "evidence": files}
with open(manifest, "w", encoding="utf-8") as f:
    json.dump(m, f, ensure_ascii=False)
print("staged %d evidence files; db sha256 %s" % (len(files), m["db_sha256"]))
'@
    [System.IO.File]::WriteAllText($stagePy, $pySource, (New-Object System.Text.UTF8Encoding($false)))

    $manifest = Join-Path $staging "manifest.json"
    Write-Host "تجهيز نسخة متسقة من القاعدة والأدلة (Python يرى OneDrive)..."
    & py -3 $stagePy $DataRoot $staging $manifest
    if ($LASTEXITCODE -ne 0) { throw "فشل التجهيز المحلي (Python)." }

    $m = Get-Content -LiteralPath $manifest -Raw -Encoding UTF8 | ConvertFrom-Json
    $count = @($m.evidence).Count
    Write-Host "جُهّزت $count ملف أدلة؛ بصمة القاعدة $($m.db_sha256)"

    # --- إنشاء المجلدات البعيدة (يُشغّل الماكينة إن كانت متوقفة) ---
    # mkdir -p ينشئ الآباء تلقائياً، فنحتاج الأب المباشر لكل دليل فقط.
    $dirs = @("/data/evidence")
    foreach ($e in $m.evidence) {
        $idx = $e.rel.LastIndexOf('/')
        if ($idx -gt 0) { $dirs += "/data/evidence/" + $e.rel.Substring(0, $idx) }
    }
    $dirs = $dirs | Sort-Object -Unique
    Write-Host "إنشاء $($dirs.Count) مجلداً على القرص..."
    $mkdir = "mkdir -p " + (($dirs | ForEach-Object { "'$_'" }) -join " ")
    & $flyExe ssh console --app $App -C $mkdir
    if ($LASTEXITCODE -ne 0) { throw "فشل إنشاء المجلدات على القرص." }

    # --- رفع القاعدة عبر sftp put المباشر ---
    $stagedDb = Join-Path $staging "zu_auditintel.db"
    Write-Host "رفع القاعدة إلى /data/zu_auditintel.db..."
    & $flyExe ssh sftp put $stagedDb "/data/zu_auditintel.db" --app $App -q
    if ($LASTEXITCODE -ne 0) { throw "فشل رفع القاعدة عبر sftp put." }

    # --- رفع الأدلة ملفاً ملفاً (put المفرد لا يرفض مجلداً موجوداً كما يفعل -R) ---
    $i = 0
    foreach ($e in $m.evidence) {
        $i++
        $local = Join-Path (Join-Path $staging "evidence") $e.rel.Replace('/', '\')
        $remote = "/data/evidence/" + $e.rel
        & $flyExe ssh sftp put $local $remote --app $App -q
        if ($LASTEXITCODE -ne 0) { throw "فشل رفع الدليل $($e.rel) عبر sftp put." }
        if ($i % 10 -eq 0 -or $i -eq $count) { Write-Host "  رُفع $i / $count" }
    }

    # --- تحقق نهائي: البصمة والعدد على القرص مقابل البيان ---
    $remoteOut = & $flyExe ssh console --app $App -C "sha256sum /data/zu_auditintel.db && find /data/evidence -type f | wc -l"
    if ($LASTEXITCODE -ne 0) { throw "فشل التحقق على القرص." }
    $remoteText = ($remoteOut -join "`n")
    if ($remoteText -notmatch $m.db_sha256) { throw "بصمة القاعدة على القرص لا تطابق المحلية: $remoteText" }
    if ($remoteText -notmatch "(?m)^\s*$count\s*$") { throw "عدد ملفات الأدلة على القرص لا يطابق $count : $remoteText" }

    Write-Host "تم الرفع والتحقق: $count ملف أدلة، قاعدة ببصمة $($m.db_sha256)"
}
finally {
    Remove-Item -LiteralPath $staging -Recurse -Force -ErrorAction SilentlyContinue
}