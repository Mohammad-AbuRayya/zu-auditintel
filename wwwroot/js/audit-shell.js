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

// طيّ الشريط الجانبي: الصنف يُوضع على <html> فينجو من إعادة رسم بلazor للتنقّل، والخيار يبقى محفوظاً.
const zuSideKey = 'zu-side-collapsed';

window.zuApplySide = () => {
    let collapsed = false;
    try { collapsed = localStorage.getItem(zuSideKey) === '1'; } catch { collapsed = false; }
    document.documentElement.classList.toggle(zuSideKey, collapsed);
};

window.zuToggleSide = () => {
    const collapsed = !document.documentElement.classList.contains(zuSideKey);
    document.documentElement.classList.toggle(zuSideKey, collapsed);
    try { localStorage.setItem(zuSideKey, collapsed ? '1' : '0'); } catch { /* التخزين معطّل في المتصفح */ }
};

window.zuApplySide();

// إضاءة الموقع: السمة صفة على <html> ضبطها سكرِبت رأس الصفحة قبل الرسم، فالتبديل هنا يقلبها ويحفظ خيارها.
window.zuToggleTheme = () => {
    const next = document.documentElement.getAttribute('data-bs-theme') === 'dark' ? 'light' : 'dark';
    document.documentElement.setAttribute('data-bs-theme', next);
    try { localStorage.setItem('zu-theme', next); } catch { /* التخزين معطّل في المتصفح */ }
};

// لغة الموقع: الخيار يُكتب في كوكي ثم تُعاد الصفحة، لأن الشاشات الثابتة ودائرة بلazor معاً
// تقرأ اللغة من الطلب، فلو بدّلناها من داخل الدائرة وحدها بقي نص الشريط على حاله.
window.zuToggleLanguage = () => {
    const next = document.documentElement.lang === 'en' ? 'ar' : 'en';
    document.cookie = `zu-lang=${next};path=/;max-age=315360000;SameSite=Lax`;
    window.location.reload();
};

// نافذة المحادثة: تُمرَّر إلى آخر رسالة بعد الفتح أو الإرسال، فالجديد أسفلها لا في أعلاها.
window.zuScrollToEnd = (id) => {
    const element = document.getElementById(id);
    if (element) element.scrollTop = element.scrollHeight;
};
