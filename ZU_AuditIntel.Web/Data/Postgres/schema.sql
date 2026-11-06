-- خريطة PostgreSQL للتطبيق: مصدر واحد يُنفَّذ بأداة الإعداد، لا ترحيلات ولا طبقة توافق.
-- الصياغات العربية تدخل كما هي: لا تحويل ولا اقتطاع في أي موضع هنا.

-- 1) الكتالوج الثابت: لا تكتب فيه الواجهة إطلاقاً، ومصدره قائمة الفحص ZU/QP02F003.
create table if not exists departments (
    department_id   text primary key,
    department_name text not null,
    item_count      int  not null default 0
);

-- صفحة ملف التدقيق مضمّنة في البند نفسه بأربعة حقول نصية، لا في جدول يُطابق بالاسم:
-- المطابقةByName كانت ثغرة المصدر في زمن EF Core، والربط بالمفتاح وحده يمنعها.
create table if not exists audit_items (
    id                       int  primary key,
    department_id            text not null references departments on delete cascade,
    category                 text not null,
    title                    text not null,
    iso_reference            text not null,
    workflow                 text not null,
    sort_order               int  not null,
    instructions_evidence    text not null default '',
    internal_audit_evidence  text not null default '',
    instructions_workflow    text not null default '',
    instructions_questions   text not null default ''
);
create index if not exists audit_items_board on audit_items (department_id, sort_order);

-- الأدلة والأسئلة: ترتيبها جزء من نصّها، فالسطر بلا sort_order يفقد موضعه في القائمة.
create table if not exists evidence_requirements (
    id           bigserial primary key,
    audit_item_id int  not null references audit_items on delete cascade,
    text         text not null,
    sort_order   int  not null
);
create index if not exists evidence_requirements_item on evidence_requirements (audit_item_id, sort_order);

create table if not exists questions (
    id           bigserial primary key,
    audit_item_id int  not null references audit_items on delete cascade,
    text         text not null,
    sort_order   int  not null
);
create index if not exists questions_item on questions (audit_item_id, sort_order);

-- 2) الحسابات: الرقم الوظيفي مفتاحاً، لا هوية داخلية، لأن أداة الزرع تقارن به الناقص وحده.
create table if not exists users (
    employee_id          text primary key,
    full_name            text not null,
    job_title            text not null default '',
    adjective            text not null default '',
    role                 text not null,
    department_id        text references departments on delete set null,
    password_salt        text not null,
    password_hash        text not null,
    password_iterations  int  not null,
    must_change_password boolean not null
);
create index if not exists users_department on users (department_id);

-- 3) ملفات الأدلة وقراراتها. المفتاح opaque كما هو اليوم، فنقاط التنزيل تأخذ حرفياً
-- ما تراه في الوثيقة ولا تعيد حساب رقم ترتيبّي قد يتصادم بين رفعين.
-- الأوقات timestamptz بالمختص العالمي، والتحويل إلى توقيت عمّان عند الحدّ لا في المخزن.
create table if not exists evidence_submissions (
    id                  text primary key,
    department_id       text not null references departments on delete cascade,
    audit_item_id       int  not null references audit_items on delete cascade,
    file_name           text not null,
    extension           text not null,
    file_size           bigint not null,
    stored_path         text not null,
    uploader_employee_id text not null references users on delete restrict,
    uploader_name       text not null,
    uploaded_at         timestamptz not null,
    agent_score         int,
    agent_defect_rate   int,
    agent_outcome       text not null,
    agent_findings      text,
    agent_checked_at    timestamptz,
    provided_evidence   text,
    final_outcome       text not null,
    final_note          text,
    reviewer_employee_id text references users on delete set null,
    reviewer_name       text,
    reviewed_at         timestamptz
);
create index if not exists submissions_department on evidence_submissions (department_id);
create index if not exists submissions_item on evidence_submissions (audit_item_id);
-- سجل المعتمد نهائياً وصندوق شاشة النتيجة: قرار التدقيق هو مرشّح الصفّ في كل مرة.
create index if not exists submissions_final on evidence_submissions (final_outcome, reviewed_at desc);

-- 4) الإشعارات: الجمهور يُكتب وقت الحدث لا وقت القراءة، ففلتر text[] يعطي عزل الجهة
-- نفسه الذي كان من جدول الربط، وبلا قراءة سجلات لكل قارئ.
create table if not exists notifications (
    id             bigserial primary key,
    kind           text not null,
    department_id  text not null,
    department_name text not null,
    item_title     text not null,
    file_name      text,
    actor_name     text,
    agent_score    int,
    note           text,
    created_at     timestamptz not null,
    visible_to     text[] not null default '{}',
    read_by        text[] not null default '{}'
);
create index if not exists notifications_visible on notifications using gin (visible_to);
create index if not exists notifications_newest on notifications (created_at desc, id desc);

-- 5) المحادثات: مفتاح الزوج المرتّب ترتيباً حرفياً هو المفتاح الطبيعي، ففتح محادثة
-- بين رقمين لا يولّد نسخة ثانية مهما تكرّر الضغط.
create table if not exists threads (
    pair_key              text primary key,
    employee_id_a         text not null references users on delete cascade,
    employee_id_b         text not null references users on delete cascade,
    created_at            timestamptz not null,
    last_message_at       timestamptz not null,
    last_body             text not null default '',
    last_sender_employee_id text not null,
    unread_for_a          int not null default 0,
    unread_for_b          int not null default 0
);
create index if not exists threads_a on threads (employee_id_a, last_message_at desc);
create index if not exists threads_b on threads (employee_id_b, last_message_at desc);

create table if not exists messages (
    id                 bigserial primary key,
    thread_pair_key    text not null references threads on delete cascade,
    sender_employee_id text not null,
    sender_name        text not null,
    body               text not null,
    sent_at            timestamptz not null
);
create index if not exists messages_thread on messages (thread_pair_key, sent_at, id);
