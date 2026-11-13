# نشر ZU_AuditIntel على Fly.io في طور التخزين المحلي: لا سرود سحابية بعد الآن.
# لم يبقَ سرّ يُضبط: مسارات القاعدة والأدلة ومفاتيح الحماية قيمُ بيئة في fly.toml.
# وأول نشرٍ هنا يزيل سرّ قائمة السماح القديم (Audit__LoginAllowlist) مرحّلاً، فكل الحسابات
# تدخل بحرية؛ والكود نفسه لم يعد يقرأ القائمة، فإزالتها تنظيفٌ لا شرطٌ للدخول.
#
# ترتيب الطور المحلي مهم: ارفع البيانات إلى القرص الدائم أولاً (deploy/upload-data.ps1)
# والماكينة القديمة ما زالت تعمل، فهي لا تقرأ /data/zu_auditintel.db إطلاقاً؛ ثم انشر
# الصورة الجديدة فتجد القاعدة جاهزة ولا تزرع فوقها شيئاً.
#
# من جذر المستودع (flyctl موقّع بـ fly auth login):
#   powershell -ExecutionPolicy Bypass -File deploy/upload-data.ps1
#   powershell -ExecutionPolicy Bypass -File deploy/fly.ps1
#
# و -SecretsOnly يزيل سرّ قائمة السماح مرحّلاً بلا بناءٍ ولا نشر، فيُطبَّق مع أول نشرٍ تالٍ.

param(
    [string]$App = "zu-auditintel",
    [string]$RepoRoot = (Split-Path $PSScriptRoot -Parent),
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

# الجذر وسيطٌ يمرّره المستخدم، ففحصه هنا يمنع بناء سياقٍ ناقصٍ على باني Fly.
foreach ($file in @("fly.toml", "Dockerfile")) {
    if (-not (Test-Path -LiteralPath (Join-Path $RepoRoot $file))) {
        throw "جذر المستودع لا يحوي $file — مرّر -RepoRoot صحيحاً: $RepoRoot"
    }
}

Push-Location -LiteralPath $RepoRoot
try {
    # إزالة سرّ قائمة السماح القديم مرحّلةً (--stage): لا إعادة تشغيل الآن، بل تُطبَّق مع النشر.
    # الكود لم يعد يقرأ Audit:LoginAllowlist، فغياب السرّ أو فشله لا يمنع النشر ولا يقيّد الدخول.
    Write-Host "إزالة سرّ قائمة السماح القديم على $App مرحّلةً..."
    & $flyExe secrets unset Audit__LoginAllowlist --app $App --stage
    if ($LASTEXITCODE -ne 0) {
        Write-Host "تعذّرت إزالة السرّ (قد يكون غير مضبوط أصلاً): يُتابَع، فالكود لم يعد يقرأه."
    }

    if ($SecretsOnly) {
        Write-Host "أُزيل سرّ قائمة السماح مرحّلاً: يُطبَّق مع أول نشر. لم يُبنَ شيء."
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