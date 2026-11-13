using Grpc.Core;

namespace ZU_AuditIntel.Web.Data
{
    /// <summary>
    /// رفضت Firestore النداء لحصةٍ نافدة أو اتصالٍ مقطوع. رسالةٌ عربية مفهومة تُعرض في الشاشة
    /// بدل نصّ gRPC الإنجليزي، وتُمسك في نقطة الدخول فلا تُرمى فيموت المضيف أو تتجمّد دائرة Blazor.
    /// </summary>
    public sealed class FirestoreUnavailableException(string operation)
        : Exception($"خدمة البيانات السحابية غير متاحة الآن، فتعذّر «{operation}». حاول مرة أخرى بعد قليل.")
    {
        public string Operation { get; } = operation;
    }

    /// <summary>
    /// بوابة صحة Firestore الواحدة في التطبيق. كل قراءة سحابية تمرّ من هنا: إن رُدّت بـ 429
    /// (ResourceExhausted) أو بأي خطأ gRPC آخر، سُجّل تحذير وقُدّم المخبأ المحلي بدل أن يُرمى
    /// الاستثناء فيموت المضيف قبل أن يستمع للمنفذ، أو تموت دائرة Blazor فتتجمّد أزرار الصفحة.
    ///
    /// العودة إلى السحابة تلقائية بلا إعادة تشغيل ولا نشر: بعد مهلة التهدئة تُجرَّب القراءة
    /// السحابية في أول عملية تالية، فإن نجحت سقط وضع التدهور وعاد التطبيق حيّاً. والمهلة قصيرة
    /// لأن الطلب المردود لا يُحسب قراءةً ناجحة ولا يستهلك من الحصة، فطولها يؤخّر الشفاء وحده.
    /// </summary>
    public sealed class FirestoreResilience(ILogger<FirestoreResilience> logger)
    {
        private static readonly TimeSpan Cooldown = TimeSpan.FromMinutes(2);

        private readonly object gate = new();
        private DateTimeOffset retryAt = DateTimeOffset.MinValue;
        private volatile bool degraded;

        /// <summary>هل القراءة الجارية تُقدَّم من المخبأ لا من السحابة؟ به تظهر لافتة الوضع المتدهور.</summary>
        public bool IsDegraded => degraded;

        /// <summary>هل حان وقت تجربة السحابة؟ تُستدعى قبل عمليةٍ لها نسخةٌ مخبوءة تُعاد المحاولة بعدها.</summary>
        public bool ShouldAttemptCloud
        {
            get
            {
                lock (gate)
                {
                    return !degraded || DateTimeOffset.UtcNow >= retryAt;
                }
            }
        }

        /// <summary>
        /// قراءة واحدة بحارسها: السحابة أولاً ما دامت مُجابة، والمخبأ المحلي متى رُدّت. الإلغاء
        /// ليس عطلاً فلا يُبتلع: صاحب الطلب الذي أغلق متصفحه يُرمى إلغاؤه كما كان.
        /// </summary>
        public async Task<T> ReadAsync<T>(string operation, Func<Task<T>> cloud, Func<Task<T>> fallback)
        {
            if (ShouldAttemptCloud)
            {
                try
                {
                    var value = await cloud();
                    MarkLive();
                    return value;
                }
                catch (RpcException error) when (error.StatusCode != StatusCode.Cancelled)
                {
                    MarkFailed(operation, error);
                }
            }

            return await fallback();
        }

        /// <summary>
        /// كتابة لا تحتمل التأجيل ولا النسخة المحلية: تُرفض ما دام الوضع متدهوراً، فلا تُطبَّق
        /// نصفَ تطبيق ولا يفترق ما على القرص عمّا في السحابة، ثم يظهر الفرقُ بغتةً عند الشفاء.
        /// </summary>
        private void EnsureWritable(string operation)
        {
            if (IsDegraded)
                throw new FirestoreUnavailableException(operation);
        }

        /// <summary>
        /// كتابة أمرٍ بيد صاحب الشاشة: رفع ملف، توقيع قرار، إرسال رسالة. تُرفض قبل أن تُجرَّب ما دام
        /// الوضع متدهوراً، وإن رُدّت من السحابة تحوّل رفضُها إلى الرسالة العربية نفسها، فتُمسك في
        /// الشاشة ويظهر نصّها لصاحبها بدل أن يموت الحدث وتتجمّد أزرار الدائرة كلها.
        /// </summary>
        public Task WriteAsync(string operation, Func<Task> write) =>
            WriteAsync<object?>(operation, async () =>
            {
                await write();
                return null;
            });

        /// <summary>والكتابة التي ترجع قيمةً كغيرها: الحارس واحد، والرسالة عند الرفض واحدة.</summary>
        public async Task<T> WriteAsync<T>(string operation, Func<Task<T>> write)
        {
            EnsureWritable(operation);

            try
            {
                var value = await write();
                MarkLive();
                return value;
            }
            catch (RpcException error) when (error.StatusCode != StatusCode.Cancelled)
            {
                MarkFailed(operation, error);
                throw new FirestoreUnavailableException(operation);
            }
        }

        /// <summary>
        /// كتابة خلفية لا يقف عليها قرار صاحب الشاشة: تعليم إشعارٍ مقروءاً، أو سطر سجلٍ عن حدثٍ وقع.
        /// إن رُدّت سُجّل التحذير وبقي الحال على ما هو، فلا يفشل رفعُ ملفٍ لأن سطر إشعاره لم يُكتب،
        /// ولا تسقط صفحةٌ لأن شارة «غير المقروء» لم تُصفَّر.
        /// </summary>
        public async Task WriteQuietlyAsync(string operation, Func<Task> write)
        {
            if (IsDegraded)
                return;

            try
            {
                await write();
            }
            catch (RpcException error) when (error.StatusCode != StatusCode.Cancelled)
            {
                MarkFailed(operation, error);
            }
        }

        public void MarkLive()
        {
            lock (gate)
            {
                degraded = false;
                retryAt = DateTimeOffset.MinValue;
            }
        }

        public void MarkFailed(string operation, RpcException error)
        {
            lock (gate)
            {
                degraded = true;
                retryAt = DateTimeOffset.UtcNow + Cooldown;
            }

            logger.LogWarning(
                error,
                "رُدّ نداء «{Operation}» إلى Firestore ({Status}: {Detail}). القراءات تُقدَّم من المخبأ المحلي، " +
                "والكتابات موقوفة، وتُعاد المحاولة السحابية بعد {Cooldown} دقيقة بلا إعادة تشغيل.",
                operation, error.StatusCode, error.Status.Detail, Cooldown.TotalMinutes);
        }
    }
}