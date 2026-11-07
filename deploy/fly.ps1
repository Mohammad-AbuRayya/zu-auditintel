# نشر ZU_AuditIntel على Fly.io: يضبط السرود ثم يبني على باني Fly البعيد وينشر.
# السرود تُقرأ من ملفات المفاتيح خارج المستودع، ولا تُطبع ولا تدخل المستودع.
#
# من جذر المستودع (flyctl موقّع بـ fly auth login):
#   powershell -ExecutionPolicy Bypass -File deploy/fly.ps1
#
# وعلى جهازٍ آخر تُمرَّر المسارات بدل الافتراضات:
#   powershell -ExecutionPolicy Bypass -File deploy/fly.ps1 -App zu-auditintel `
#       -RepoRoot D:/zu-auditintel -FirestoreKeyFile C:/keys/serviceAccountKey.json `
#       -B2KeyFile C:/keys/b2.json
#
# و -SecretsOnly يضبط السرود مرحّلةً بلا بناءٍ ولا نشر، فتُطبَّق مع أول نشرٍ تالٍ.

param(
    [string]$App = "zu-auditintel",
    [string]$RepoRoot = (Split-Path $PSScriptRoot -Parent),
    [string]$FirestoreKeyFile = "${env:USERPROFILE}\.config\zu-auditintel\serviceAccountKey.json",
    [string]$B2KeyFile = "${env:USERPROFILE}\.config\zu-auditintel\b2.json",
    [switch]$SecretsOnly
)

$ErrorActionPreference = "Stop"

$flyExe = $null
foreach ($name in @("flyctl", "fly")) {
    $command = Get-Command $name -ErrorAction SilentlyContinue
    if ($command) { $flyExe = $command.Source; break }
}
if (-not $flyExe) {
    $candidate = Join-Path $env:USERPROFILE ".fly\bin\flyctl.exe"
    if (Test-Path -LiteralPath $candidate) { $flyExe = $candidate }
}
if (-not $flyExe) {
    throw "flyctl غير منصوب: شغّل powershell -Command `"iwr https://fly.io/install.ps1 -useb | iex`" ثم أعد المحاولة."
}

$RepoRoot = (Resolve-Path -LiteralPath $RepoRoot).Path

foreach ($file in @($FirestoreKeyFile, $B2KeyFile)) {
    if (-not (Test-Path -LiteralPath $file)) { throw "ملف المفتاح غير موجود: $file" }
}

# الجذر وسيطٌ يمرّره المستخدم، ففحصه هنا يمنع بناء سياقٍ ناقصٍ على باني Fly.
foreach ($file in @("fly.toml", "Dockerfile")) {
    if (-not (Test-Path -LiteralPath (Join-Path $RepoRoot $file))) {
        throw "جذر المستودع لا يحوي $file — مرّر -RepoRoot صحيحاً: $RepoRoot"
    }
}

# المفتاح يُضغط في سطر واحد: قيمته متعددة الأسطر تربك حقن متغيّرات البيئة، والمحتوى لا يتغيّر.
$firestoreJson = Get-Content -LiteralPath $FirestoreKeyFile -Raw | ConvertFrom-Json | ConvertTo-Json -Depth 20 -Compress
$b2 = Get-Content -LiteralPath $B2KeyFile -Raw | ConvertFrom-Json

# فحصٌ قبل الإرسال: PowerShell 5.1 يجرد علامات الاقتباس من وسيطٍ نصّي حين يمرّره إلى exe،
# فتصل القيمة JSON بلا اقتباسات وينهار الإقلاع. ما يُمرَّر هنا stdin لا وسيطاً، والفحص يمسك
# أي فسادٍ آخر قبل أن يبلغ السحابة.
if ($firestoreJson -notmatch '"type"\s*:\s*"service_account"') {
    throw "مفتاح Firestore لم يعد JSON صالحاً بعد التحويل — توقّف قبل الإرسال."
}

$secretPairs = @(
    "Firestore__CredentialsJson=$firestoreJson",
    "B2_ACCESS_KEY_ID=$($b2.accessKeyId)",
    "B2_SECRET_ACCESS_KEY=$($b2.secretAccessKey)",
    "Audit__LoginAllowlist=9999999"
)

Push-Location -LiteralPath $RepoRoot
try {
    Write-Host "ضبط السرود على $App عبر stdin (مرحلةٌ واحدة بلا إعادة تشغيل)..."

    # ملف مؤقت بايتاتُه تصل stdin كما هي: Start-Process لا يعيد ترميزاً ولا يضيف \r، فالاقتباسات
    # والأسطر المحرفة في المفتاح تبقى سليمة. ويُحذف في finally فلا يبقى سرّ على القرص.
    $secretFile = Join-Path ([System.IO.Path]::GetTempPath()) ("fly-secrets-" + [guid]::NewGuid().ToString("N") + ".env")
    try {
        [System.IO.File]::WriteAllText(
            $secretFile,
            ($secretPairs -join "`n") + "`n",
            (New-Object System.Text.UTF8Encoding($false)))

        $process = Start-Process -FilePath $flyExe `
            -ArgumentList @("secrets", "import", "--app", $App, "--stage") `
            -RedirectStandardInput $secretFile -NoNewWindow -Wait -PassThru
        if ($process.ExitCode -ne 0) { throw "فشل ضبط السرود (exit $($process.ExitCode))." }
    }
    finally {
        if (Test-Path -LiteralPath $secretFile) { Remove-Item -LiteralPath $secretFile -Force }
    }

    if ($SecretsOnly) {
        Write-Host "السرود مضبوطة ومرحّلة: تُطبَّق مع أول نشر. لم يُبنَ شيء."
        return
    }

    Write-Host "البناء على باني Fly البعيد ثم النشر..."
    & $flyExe deploy --app $App --remote-only --ha=false
    if ($LASTEXITCODE -ne 0) { throw "فشل النشر (exit $LASTEXITCODE)." }

    & $flyExe status --app $App
    Write-Host "الرابط: https://$App.fly.dev"
}
finally {
    Pop-Location
}