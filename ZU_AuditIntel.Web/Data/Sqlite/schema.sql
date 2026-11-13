-- خريطة SQLite المحلية: مصدر واحد يقرأه التطبيق عند الإقلاع وأداة الاستيراد معاً، فلا مخططَين يتباعدان.
-- الصياغات العربية تدخل كما هي: لا تحويل ولا اقتطاع في أي موضع هنا.
-- الأوقات تُخزَّن UTC بصيغة round-trip قابلة للفرز نصياً، وتُردّ إلى توقيت الجهاز عند القراءة.

-- 1) الكتالوج الثابت: لا تكتب فيه الواجهة إطلاقاً، ومصدره قائمة الفحص ZU/QP02F003.
create table if not exists departments (
    department_id   text primary key,
    department_name text not null,
    item_count      integer not null default 0
);

-- الأدلة والأسئلة مصفوفتان داخل البند نفسه كما كانتا في الوثيقة: ترتيبها جزء من نصّها،
-- وقراءتها قراءةٌ واحدة بلا جدول مقابل يُطابق بالاسم.
create table if not exists audit_items (
    id                          integer primary key,
    department_id               text not null references departments(department_id) on delete cascade,
    category                    text not null,
    title                       text not null,
    iso_reference               text not null,
    workflow                    text not null,
    sort_order                  integer not null,
    required_evidence           text not null default '[]',
    questions                   text not null default '[]',
    sheet_instructions_evidence   text not null default '',
    sheet_internal_audit_evidence text not null default '',
    sheet_instructions_workflow   text not null default '',
    sheet_instructions_questions  text not null default ''
);
create index if not exists audit_items_board on audit_items (department_id, sort_order, id);

-- 2) الحسابات: الرقم الوظيفي مفتاحاً، ورقم السطر موروثٌ فريد لأن الجلسة تحمله لا الرقم الوظيفي.
create table if not exists users (
    employee_id          text primary key,
    id                   integer not null unique,
    full_name            text not null,
    job_title            text not null default '',
    adjective            text not null default '',
    role                 text not null,
    department_id        text references departments(department_id) on delete set null,
    password_salt        text not null,
    password_hash        text not null,
    password_iterations  integer not null,
    must_change_password integer not null
);
create index if not exists users_department on users (department_id);

-- 3) ملفات الأدلة وقراراتها. stored_path مفتاحٌ نسبي {جهة}/{بند}/{اسم مولّد}{امتداد} كما كان،
-- فجذر التخزين وحده يتغيّر بالانتقال إلى القرص ولا تتغيّر دلالة المفتاح ولا صفوفه.
create table if not exists evidence_submissions (
    id                   text primary key,
    department_id        text not null references departments(department_id) on delete cascade,
    audit_item_id        integer not null references audit_items(id) on delete cascade,
    file_name            text not null,
    extension            text not null,
    file_size            integer not null,
    stored_path          text not null,
    uploader_employee_id text not null,
    uploader_name        text not null,
    uploaded_at          text not null,
    agent_score          integer,
    agent_defect_rate    integer,
    agent_outcome        text not null,
    agent_findings       text,
    agent_checked_at     text,
    provided_evidence    text,
    final_outcome        text not null,
    final_note           text,
    reviewer_employee_id text,
    reviewer_name        text,
    reviewed_at          text
);
create index if not exists submissions_department on evidence_submissions (department_id);
create index if not exists submissions_item on evidence_submissions (audit_item_id);
create index if not exists submissions_agent on evidence_submissions (agent_outcome);
create index if not exists submissions_final on evidence_submissions (final_outcome, reviewed_at desc);

-- 4) الإشعارات: دوائر الرؤية وقُرّاء السطر جدولان لا مصفوفتان، ففلتر الجهة وعدّاد غير المقروء
-- وتعليم القراءة كلها استعلامات مفهرسة بلا مسح للسجل ولا سقفٍ على عدد الكتابات.
create table if not exists notifications (
    id              text primary key,
    kind            text not null,
    department_id   text not null,
    department_name text not null,
    item_title      text not null,
    file_name       text,
    actor_name      text,
    agent_score     integer,
    note            text,
    created_at      text not null
);
create index if not exists notifications_newest on notifications (created_at desc, id desc);

create table if not exists notification_audience (
    circle          text not null,
    notification_id text not null references notifications(id) on delete cascade,
    primary key (circle, notification_id)
);
create index if not exists notification_audience_row on notification_audience (notification_id);

create table if not exists notification_read (
    notification_id text not null references notifications(id) on delete cascade,
    employee_id     text not null,
    primary key (notification_id, employee_id)
);
create index if not exists notification_read_employee on notification_read (employee_id);

-- 5) المحادثات: مفتاح الزوج المرتّب ترتيباً حرفياً هو المفتاح الطبيعي، ففتح محادثة بين رقمين
-- لا يولّد نسخة ثانية مهما تكرّر الضغط.
create table if not exists threads (
    pair_key                text primary key,
    employee_id_a           text not null,
    employee_id_b           text not null,
    created_at              text not null,
    last_message_at         text not null,
    last_body               text not null default '',
    last_sender_employee_id text not null default '',
    unread_for_a            integer not null default 0,
    unread_for_b            integer not null default 0
);
create index if not exists threads_a on threads (employee_id_a, last_message_at desc);
create index if not exists threads_b on threads (employee_id_b, last_message_at desc);

create table if not exists messages (
    id                 text primary key,
    thread_pair_key    text not null references threads(pair_key) on delete cascade,
    sender_employee_id text not null,
    sender_name        text not null,
    body               text not null,
    sent_at            text not null
);
create index if not exists messages_thread on messages (thread_pair_key, sent_at, id);