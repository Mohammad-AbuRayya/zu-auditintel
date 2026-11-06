# بناءٌ مرحلتان: حزمةُ إس إيكو دي تبني وتنشر، وحزمةُ تشغيلٍ خفيفة تحمل الناتج وحده.
# لا مفاتيح ولا مسارات أقراص هنا: السرود كلها قيمةُ متغيّر بيئة تعطيها المنصّة، فلا يدخل
# مستودعَ ولا صورةً ما يجوز أن يُقرأ من الصورة.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# الملف يُفرَد قبل الشجرة كلها: ذاكرةُ الطبقات تُبقي restore منفّذة ما لم تتغيّر الحزم،
# فتُعاد قراءةُ الكتالوج والواجهة في كل إصلاحٍ بلا تنزيلٍ من جديد.
COPY ZU_AuditIntel.Web/ZU_AuditIntel.Web.csproj ZU_AuditIntel.Web/
RUN dotnet restore ZU_AuditIntel.Web/ZU_AuditIntel.Web.csproj

COPY ZU_AuditIntel.Web/ ZU_AuditIntel.Web/
RUN dotnet publish ZU_AuditIntel.Web/ZU_AuditIntel.Web.csproj -c Release -o /publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /publish ./

# الإنتاج لا التطوير: وضعُ التطوير يقرأ مسارات المفاتيح من القرص المحلي ويكشف الاستثناءات.
ENV ASPNETCORE_ENVIRONMENT=Production

# المنصّة تختار المنفذ تعطيه في PORT، والافتراضي ما تعوّده Render. القشرة وحدها توسّع
# المتغيّر، فالصورة تصمت على أي منفذٍ اختاره المستضيف دون إعادة بناء.
ENV PORT=10000
EXPOSE 10000

CMD ["sh", "-c", "exec dotnet ZU_AuditIntel.Web.dll --urls=http://+:${PORT}"]
