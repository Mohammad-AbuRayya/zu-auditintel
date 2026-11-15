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
const zuDrawerKey = 'zu-drawer-open';

// الحدّ نفسه المكتوب في ورقة أنماط التخطيط: تحته الشريط درجٌ منزلق لا عموداً مطويّاً.
const zuNarrowRail = window.matchMedia('(max-width: 576px)');

window.zuApplySide = () => {
    let collapsed = false;
    try { collapsed = localStorage.getItem(zuSideKey) === '1'; } catch { collapsed = false; }
    // الطيّ لا معنى له على الجوال: الدرج يعرض الأزرار بنصوصها كاملةً، ولو بقي الصنف اختفت وهو مفتوح.
    document.documentElement.classList.toggle(zuSideKey, collapsed && !zuNarrowRail.matches);
};

window.zuCloseDrawer = () => document.documentElement.classList.remove(zuDrawerKey);

window.zuToggleSide = () => {
    const root = document.documentElement;

    // الزر نفسه يفتح الدرج ويغلقه، ولا يمسّ خيار الطيّ المحفوظ لسطح المكتب.
    if (zuNarrowRail.matches) {
        root.classList.toggle(zuDrawerKey);
        return;
    }

    const collapsed = !root.classList.contains(zuSideKey);
    root.classList.toggle(zuSideKey, collapsed);
    try { localStorage.setItem(zuSideKey, collapsed ? '1' : '0'); } catch { /* التخزين معطّل في المتصفح */ }
};

window.zuApplySide();

// عبور الحدّ يعيد ضبط الشريط: الدرج يُغلق عند الاتساع، والطيّ المحفوظ يعود ما دام محفوظاً.
zuNarrowRail.addEventListener('change', () => {
    window.zuCloseDrawer();
    window.zuApplySide();
});

document.addEventListener('keydown', (event) => {
    if (event.key === 'Escape') window.zuCloseDrawer();
});

// اختيار وجهٍ من أزرار الدرج يغلقه: بلazor يبدّل المحتوى ولا يعيد تحميل الصفحة، فيبقى الصنف على <html>.
document.addEventListener('click', (event) => {
    if (event.target.closest?.('.zu-board-side a.zu-side-item')) window.zuCloseDrawer();
});

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

// فتح جهة يستبدل الشاشة كلها: تُعاد النافذة إلى أعلاها فلا يبقى المدقّق عند تمرير الشبكة القديمة.
// «instant» صراحةً لأن بوتستراب يضبط scroll-behavior: smooth على الجذر، فتصير إعادة الموضع حركةً
// مؤجّلة تُقاطعها إعادة رسم بلazor للشاشة الجديدة وقد لا تبلغ أعلى الصفحة إطلاقاً.
window.zuScrollToTop = () => window.scrollTo({ top: 0, left: 0, behavior: 'instant' });

// النافذة المنبثقة: تُقفل تمرير الصفحة خلفها وتُنقل بؤرة لوحة المفاتيح إليها، فـ Escape يصلها.
window.zuOverlay = (id, open) => {
    document.documentElement.classList.toggle('zu-overlay-open', !!open);
    if (!open) return;
    const element = document.getElementById(id);
    if (element) element.focus();
};

// نسخة الورقة كما هي الآن: حقول الإدخال تُستبدل بنصّها المكتوب، فالملف المنزَّل يحمل ما كتبه
// المدقّق في الملخص والتوقيع لا صناديق فارغة، وما وُسم data-report-exclude لا يخرج من الشاشة.
function zuReportSnapshot(sheetId) {
    const sheet = document.getElementById(sheetId);
    if (!sheet) return null;

    const clone = sheet.cloneNode(true);
    const live = sheet.querySelectorAll('textarea, input');
    const copied = clone.querySelectorAll('textarea, input');
    live.forEach((field, index) => {
        const holder = document.createElement('div');
        holder.className = `${field.className} zu-report-static`;
        holder.textContent = field.value || '';
        copied[index].replaceWith(holder);
    });
    clone.querySelectorAll('[data-report-exclude]').forEach((node) => node.remove());
    return clone;
}

// الشعار يُضمَّن بايتاتٍ في الملف: الوثيقة تُفتح على جهاز لا يبلغ الخادم، فلا يظهر شعار مكسور.
async function zuInlineImages(root) {
    await Promise.all(Array.from(root.querySelectorAll('img')).map(async (img) => {
        try {
            const response = await fetch(img.getAttribute('src'), { cache: 'force-cache' });
            const blob = await response.blob();
            img.src = await new Promise((resolve, reject) => {
                const reader = new FileReader();
                reader.onload = () => resolve(reader.result);
                reader.onerror = reject;
                reader.readAsDataURL(blob);
            });
        } catch {
            img.remove();
        }
    }));
}

function zuSaveText(fileName, mime, text) {
    // BOM في أول الملف: بلاها يقرأ Word العربية بترميز خاطئ فتظهر حروفاً مبعثرة.
    const url = URL.createObjectURL(new Blob(['\ufeff', text], { type: mime }));
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    link.remove();
    setTimeout(() => URL.revokeObjectURL(url), 4000);
}

// تنزيل التقرير: الورقة نفسها المعروضة، مع ورقة الأنماط نفسها حرفياً، فتطابق المعاينةُ الملفَ.
// صيغة word ملف HTML بامتداد doc وترويسة MS Word: يفتحه Word مستنداً قابلاً للتحرير بلا مكتبات.
window.zuDownloadReport = async (sheetId, fileName, format, title) => {
    const clone = zuReportSnapshot(sheetId);
    if (!clone) return false;

    await zuInlineImages(clone);
    const css = await fetch('/css/audit-report.css', { cache: 'no-store' }).then((r) => r.text());
    const html = document.documentElement;
    const word = format === 'word';
    const page = word
        ? '@page WordSection1 { size: 595.3pt 841.9pt; margin: 40pt 34pt; } div.WordSection1 { page: WordSection1; }'
        : '';
    const document_ = `<!DOCTYPE html>
<html xmlns:w="urn:schemas-microsoft-com:office:word" lang="${html.lang || 'ar'}" dir="${html.dir || 'rtl'}">
<head><meta charset="utf-8"><title>${title}</title>
${word ? '<!--[if gte mso 9]><xml><w:WordDocument><w:View>Print</w:View><w:Zoom>100</w:Zoom></w:WordDocument></xml><![endif]-->' : ''}
<style>${css}\n${page}</style></head>
<body class="zu-report-export"><div class="WordSection1">${clone.outerHTML}</div></body></html>`;

    zuSaveText(fileName, word ? 'application/msword;charset=utf-8' : 'text/html;charset=utf-8', document_);
    return true;
};

// الطباعة: تُنقل نسخة الورقة إلى جذرٍ مباشر تحت body، وقاعدة @media print تخفي كل ما عداه.
// فلا يُطبع الشريط الجانبي ولا خلفية النافذة ولا أزرارها، وتخرج الورقة وحدها على A4.
window.zuPrintReport = (sheetId) => {
    const clone = zuReportSnapshot(sheetId);
    if (!clone) return false;

    const host = document.createElement('div');
    host.id = 'zu-report-print-root';
    host.setAttribute('dir', document.documentElement.dir || 'rtl');
    host.appendChild(clone);
    document.body.appendChild(host);
    document.documentElement.classList.add('zu-printing-report');

    let cleaned = false;
    const cleanup = () => {
        if (cleaned) return;
        cleaned = true;
        document.documentElement.classList.remove('zu-printing-report');
        host.remove();
        window.removeEventListener('afterprint', cleanup);
    };
    window.addEventListener('afterprint', cleanup);
    window.print();
    // شبكة أمان لمتصفح لا يطلق afterprint: الجذر مخفيّ على الشاشة بقاعدة CSS، فبقاؤه دقيقة لا يُرى.
    setTimeout(cleanup, 60000);
    return true;
};
