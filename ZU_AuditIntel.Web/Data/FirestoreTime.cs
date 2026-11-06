using Google.Cloud.Firestore;

namespace ZU_AuditIntel.Web.Data
{
    /// <summary>
    /// Firestore يحفظ اللحظة بلغة UTC، والشاشات تعرض توقيت عمّان كما كتبه صاحب الحدث: الكتابة
    /// تحوّل الوقت المحلي إلى لحظة، والقراءة تردّها إلى توقيت الجهاز. وبلا هذا المحوّل يظهر
    /// سجلّ التدقيق متأخراً ثلاث ساعات، ويُسبق إشعارٌ أخاه في القائمة.
    /// وهو محوّل الحقول القابلة للغياب أيضاً: الحزمة تُغلّف Nullable بنفسها، فتنادي هذا المحوّل
    /// بالقيمة الموجودة وحدها وترسل null حين غابت — فلا محوّل ثانٍ لحقل «لم يحدث بعد».
    /// </summary>
    public sealed class LocalTimeConverter : IFirestoreConverter<DateTime>
    {
        public object ToFirestore(DateTime value) => Timestamp.FromDateTime(value.ToUniversalTime());

        public DateTime FromFirestore(object value) => ((Timestamp)value).ToDateTimeOffset().LocalDateTime;
    }
}
