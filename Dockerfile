# بناءٌ مرحلتان: حزمةُ إس إيكو دي تبني وتنشر، وحزمةُ تشغيلٍ خفيفة تحمل الناتج وحده.
# لا مفاتيح ولا مسارات أقراص هنا: السرود كلها قيمةُ متغيّر بيئة تعطيها المنصّة، فلا يدخل
# مستودعَ ولا صورةً ما يجوز أن يُقرأ من الصورة.
# الإصدار مثبّت لا عائم: والزوج (10.0.402 / 10.0.13) هو نفسه المثبت على جهاز المطوّر، فيقوم
# البناء على أرضٍ واحدة لا على وسمٍ ينزلق تحتنا.
FROM mcr.microsoft.com/dotnet/sdk:10.0.402 AS build
WORKDIR /src

# الملف يُفرَد قبل الشجرة كلها: ذاكرةُ الطبقات تُبقي تنزيلَ الحزم منفّذاً ما لم تتغيّر الحزم،
# فتُعاد قراءةُ الكتالوج والواجهة في كل إصلاحٍ بلا تنزيلٍ من جديد.
COPY ZU_AuditIntel.Web/ZU_AuditIntel.Web.csproj ZU_AuditIntel.Web/
RUN dotnet restore ZU_AuditIntel.Web/ZU_AuditIntel.Web.csproj

COPY ZU_AuditIntel.Web/ ZU_AuditIntel.Web/

# ولا --no-restore هنا: حزمةُ سكربتات إقلاع Blazor تُضاف إلى المشروع ضمنياً حين يرى MSBuild
# مكوّنات Razor، لا حين يرى csproj وحده. فالأولُ كتب بيانَ حزمٍ بلا تلك الحزمة، ولو ورثه النشرُ
# لخلا wwwroot/_framework من blazor.web.js وخلا منه بيانُ المنافذ، فطبع الوسمُ المسارَ حرفياً
# بلا بصمة وأعاد 404 صفحةَ not-found، ولم تقم للدارة قائمة وتجمّد كل زرٍّ رغم أن الصفحة نفسها 200.
# فالنشرُ يعيد الاسترجاعَ والشجرةُ كاملةٌ حاضرة، والحزمُ نفسها في الذاكرة فلا تنزيلَ من جديد.
RUN dotnet publish ZU_AuditIntel.Web/ZU_AuditIntel.Web.csproj -c Release -o /publish

# حارسُ بناء: صورةٌ بلا السكربت في بيانها ميتةُ الأزرار، فلا معنى لنشرها ثم اكتشافها على الرابط.
RUN grep -q '_framework/blazor.web.js' /publish/ZU_AuditIntel.Web.staticwebassets.endpoints.json

FROM mcr.microsoft.com/dotnet/aspnet:10.0.13 AS runtime
WORKDIR /app
COPY --from=build /publish ./

# الإنتاج لا التطوير: وضعُ التطوير يقرأ مسارات المفاتيح من القرص المحلي ويكشف الاستثناءات.
ENV ASPNETCORE_ENVIRONMENT=Production

# المنصّة تختار المنفذ تعطيه في PORT، والافتراضي ما تعوّده Render. القشرة وحدها توسّع
# المتغيّر، فالصورة تصمت على أي منفذٍ اختاره المستضيف دون إعادة بناء.
ENV PORT=10000
EXPOSE 10000

CMD ["sh", "-c", "exec dotnet ZU_AuditIntel.Web.dll --urls=http://+:${PORT}"]