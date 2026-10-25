// الوظائف المشتركة بين الشاشات الثابتة والتفاعلية.
// تسجيل الخروج يتم عبر POST بصيغة JSON: المتصفح يمنع موقعاً آخر من إرسال هذه الصيغة
// بدون تحقق CORS، فلا حاجة إلى رمز مضاد للتزييف في الرابط أو النموذج.
window.zuSignOut = async () => {
    await fetch('/account/logout', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: '{}'
    });
    window.location.assign('/login');
};
