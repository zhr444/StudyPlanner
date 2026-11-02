-- =============================================================================
-- БАЗА ДАННЫХ: Система управления учебными задачами и дедлайнами (StudyFlow)
-- СУБД: PostgreSQL 15+
-- Автор: Захар
-- Соответствует ER-диаграмме и техническому заданию (15 таблиц)
-- =============================================================================

DROP TABLE IF EXISTS report_exports CASCADE;
DROP TABLE IF EXISTS reminders CASCADE;
DROP TABLE IF EXISTS reminder_types CASCADE;
DROP TABLE IF EXISTS tasks CASCADE;
DROP TABLE IF EXISTS admins CASCADE;
DROP TABLE IF EXISTS users CASCADE;
DROP TABLE IF EXISTS schedule CASCADE;
DROP TABLE IF EXISTS group_disciplines CASCADE;
DROP TABLE IF EXISTS groups CASCADE;
DROP TABLE IF EXISTS teacher_disciplines CASCADE;
DROP TABLE IF EXISTS teachers CASCADE;
DROP TABLE IF EXISTS disciplines CASCADE;
DROP TABLE IF EXISTS task_types CASCADE;
DROP TABLE IF EXISTS statuses CASCADE;
DROP TABLE IF EXISTS priorities CASCADE;

-- 1. Справочник приоритетов задач (priorities)
CREATE TABLE priorities (
    id SERIAL PRIMARY KEY,
    name VARCHAR(20) NOT NULL UNIQUE,
    level INT NOT NULL UNIQUE,
    color VARCHAR(7) NOT NULL
);

-- 2. Справочник статусов выполнения задач (statuses)
CREATE TABLE statuses (
    id SERIAL PRIMARY KEY,
    name VARCHAR(30) NOT NULL UNIQUE,
    color VARCHAR(7) NOT NULL,
    is_final BOOLEAN NOT NULL DEFAULT FALSE
);

-- 3. Справочник типов учебных работ (task_types)
CREATE TABLE task_types (
    id SERIAL PRIMARY KEY,
    name VARCHAR(50) NOT NULL UNIQUE
);

-- 4. Справочник учебных дисциплин (disciplines)
CREATE TABLE disciplines (
    id SERIAL PRIMARY KEY,
    name VARCHAR(150) NOT NULL UNIQUE,
    description TEXT
);

-- 5. Справочник преподавателей (teachers)
CREATE TABLE teachers (
    id SERIAL PRIMARY KEY,
    last_name VARCHAR(100) NOT NULL,
    first_name VARCHAR(100) NOT NULL,
    patronymic VARCHAR(100),
    email VARCHAR(255) UNIQUE,
    phone VARCHAR(20),
    degree VARCHAR(100)
);

-- 6. Связь преподавателей и дисциплин (teacher_disciplines)
CREATE TABLE teacher_disciplines (
    teacher_id INT NOT NULL REFERENCES teachers(id) ON DELETE CASCADE,
    discipline_id INT NOT NULL REFERENCES disciplines(id) ON DELETE CASCADE,
    PRIMARY KEY (teacher_id, discipline_id)
);

-- 7. Справочник учебных групп (groups)
CREATE TABLE groups (
    id SERIAL PRIMARY KEY,
    name VARCHAR(20) NOT NULL UNIQUE,
    course INT NOT NULL CHECK (course BETWEEN 1 AND 6),
    start_year INT NOT NULL
);

-- 8. Связь учебных групп, дисциплин и преподавателей (group_disciplines)
CREATE TABLE group_disciplines (
    group_id INT NOT NULL REFERENCES groups(id) ON DELETE CASCADE,
    discipline_id INT NOT NULL REFERENCES disciplines(id) ON DELETE CASCADE,
    teacher_id INT NOT NULL REFERENCES teachers(id) ON DELETE CASCADE,
    PRIMARY KEY (group_id, discipline_id)
);

-- 9. Расписание занятий (schedule)
CREATE TABLE schedule (
    id SERIAL PRIMARY KEY,
    group_id INT NOT NULL REFERENCES groups(id) ON DELETE CASCADE,
    discipline_id INT NOT NULL REFERENCES disciplines(id) ON DELETE CASCADE,
    teacher_id INT NOT NULL REFERENCES teachers(id) ON DELETE CASCADE,
    day_of_week INT NOT NULL CHECK (day_of_week BETWEEN 1 AND 6),
    pair_number INT NOT NULL CHECK (pair_number BETWEEN 1 AND 8),
    is_even_week BOOLEAN NOT NULL DEFAULT FALSE,
    audience VARCHAR(20) NOT NULL,
    start_time TIME NOT NULL,
    end_time TIME NOT NULL
);

-- 10. Пользователи / Студенты (users)
CREATE TABLE users (
    id SERIAL PRIMARY KEY,
    last_name VARCHAR(100) NOT NULL,
    first_name VARCHAR(100) NOT NULL,
    patronymic VARCHAR(100),
    email VARCHAR(255) NOT NULL UNIQUE,
    phone VARCHAR(20),
    password_hash VARCHAR(255) NOT NULL,
    group_id INT NOT NULL REFERENCES groups(id) ON DELETE RESTRICT,
    registration_date TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    is_admin BOOLEAN NOT NULL DEFAULT FALSE,
    is_blocked BOOLEAN NOT NULL DEFAULT FALSE,
    block_reason TEXT
);

-- 11. Администраторы системы (admins)
CREATE TABLE admins (
    id SERIAL PRIMARY KEY,
    login VARCHAR(255) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    full_name VARCHAR(200) NOT NULL,
    is_super_admin BOOLEAN NOT NULL DEFAULT FALSE,
    last_login_at TIMESTAMP
);

-- 12. Учебные задачи студентов (tasks)
CREATE TABLE tasks (
    id SERIAL PRIMARY KEY,
    title VARCHAR(200) NOT NULL,
    description TEXT,
    deadline TIMESTAMP NOT NULL,
    priority_id INT NOT NULL REFERENCES priorities(id) ON DELETE RESTRICT,
    status_id INT NOT NULL REFERENCES statuses(id) ON DELETE RESTRICT,
    type_id INT NOT NULL REFERENCES task_types(id) ON DELETE RESTRICT,
    discipline_id INT NOT NULL REFERENCES disciplines(id) ON DELETE CASCADE,
    user_id INT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    schedule_id INT REFERENCES schedule(id) ON DELETE SET NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    completed_at TIMESTAMP
);

-- 13. Типы напоминаний о дедлайнах (reminder_types)
CREATE TABLE reminder_types (
    id SERIAL PRIMARY KEY,
    name VARCHAR(50) NOT NULL UNIQUE,
    offset_seconds INT NOT NULL
);

-- 14. Запланированные и отправленные напоминания (reminders)
CREATE TABLE reminders (
    id SERIAL PRIMARY KEY,
    task_id INT NOT NULL REFERENCES tasks(id) ON DELETE CASCADE,
    user_id INT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    reminder_type_id INT NOT NULL REFERENCES reminder_types(id) ON DELETE RESTRICT,
    scheduled_at TIMESTAMP NOT NULL,
    sent_at TIMESTAMP,
    is_sent BOOLEAN NOT NULL DEFAULT FALSE
);

-- 15. История экспорта отчётов об успеваемости (report_exports)
CREATE TABLE report_exports (
    id SERIAL PRIMARY KEY,
    user_id INT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    format VARCHAR(10) NOT NULL CHECK (format IN ('XLSX', 'PDF')),
    period_from DATE NOT NULL,
    period_to DATE NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Индексы для ускорения выборок и поиска
CREATE INDEX idx_tasks_user_deadline ON tasks(user_id, deadline);
CREATE INDEX idx_tasks_discipline ON tasks(discipline_id);
CREATE INDEX idx_tasks_status ON tasks(status_id);
CREATE INDEX idx_schedule_group_week ON schedule(group_id, is_even_week, day_of_week);
CREATE INDEX idx_reminders_pending ON reminders(scheduled_at, is_sent) WHERE is_sent = FALSE;

-- =============================================================================
-- НАЧАЛЬНОЕ НАПОЛНЕНИЕ СПРАВОЧНИКОВ И ТЕСТОВЫХ ДАННЫХ (SEED DATA)
-- =============================================================================

-- Приоритеты по ТЗ: Низкий, Средний, Высокий, Критический
INSERT INTO priorities (id, name, level, color) VALUES
(1, 'Низкий', 1, '#10B981'),
(2, 'Средний', 2, '#3B82F6'),
(3, 'Высокий', 3, '#F59E0B'),
(4, 'Критический', 4, '#EF4444');

-- Статусы по ТЗ: К выполнению, В процессе, Сдано, Просрочено
INSERT INTO statuses (id, name, color, is_final) VALUES
(1, 'К выполнению', '#64748B', FALSE),
(2, 'В процессе', '#2563EB', FALSE),
(3, 'Сдано', '#16A34A', TRUE),
(4, 'Просрочено', '#DC2626', FALSE);

-- Типы учебных работ по ТЗ: Лабораторная, Практическая, Курсовая
INSERT INTO task_types (id, name) VALUES
(1, 'Лабораторная работа'),
(2, 'Практическая работа'),
(3, 'Курсовой проект'),
(4, 'Расчётно-графическая работа'),
(5, 'Подготовка к зачёту / экзамену');

-- Типы напоминаний по ТЗ: за 3 дня, за 24 часа, в день наступления дедлайна
INSERT INTO reminder_types (id, name, offset_seconds) VALUES
(1, 'За 3 дня до дедлайна', 259200),
(2, 'За 24 часа до дедлайна', 86400),
(3, 'В день дедлайна (утром)', 10800);

-- Учебные дисциплины
INSERT INTO disciplines (id, name, description) VALUES
(1, 'Проектирование и разработка веб-приложений', 'Разработка серверных приложений на C# ASP.NET Core, REST API и фронтенда.'),
(2, 'Системы управления базами данных', 'Реляционная алгебра, проектирование ER-моделей, язык SQL и СУБД PostgreSQL.'),
(3, 'Инженерия программного обеспечения', 'Жизненный цикл ПО, UML-моделирование (Use Case, Activity), составление ТЗ и тестирование.'),
(4, 'Алгоритмы и структуры данных', 'Оценка сложности алгоритмов, графы, деревья поиска, динамическое программирование.'),
(5, 'Компьютерные сети и телекоммуникации', 'Стек протоколов TCP/IP, маршрутизация, сетевая безопасность и HTTP/HTTPS.');

-- Преподаватели
INSERT INTO teachers (id, last_name, first_name, patronymic, email, phone, degree) VALUES
(1, 'Соколов', 'Андрей', 'Викторович', 'sokolov.av@uni.edu.ru', '+7 (916) 240-11-22', 'к.т.н., доцент'),
(2, 'Морозова', 'Елена', 'Сергеевна', 'morozova.es@uni.edu.ru', '+7 (916) 355-44-18', 'к.ф.-м.н., доцент'),
(3, 'Волков', 'Дмитрий', 'Игоревич', 'volkov.di@uni.edu.ru', '+7 (925) 780-90-05', 'ст. преподаватель'),
(4, 'Кравцова', 'Ольга', 'Николаевна', 'kravtsova.on@uni.edu.ru', '+7 (903) 512-33-67', 'д.т.н., профессор');

-- Связь преподавателей и дисциплин
INSERT INTO teacher_disciplines (teacher_id, discipline_id) VALUES
(1, 1), (1, 3),
(2, 2),
(3, 4), (3, 5),
(4, 3);

-- Учебные группы
INSERT INTO groups (id, name, course, start_year) VALUES
(1, 'ИСП-23', 4, 2023),
(2, 'ИСП-24', 3, 2024),
(3, 'ТМ-26', 1, 2026),
(4, 'Л-24', 3, 2024),
(5, 'Л-25', 2, 2025),
(6, 'Р-25', 2, 2025),
(7, 'Р-26', 1, 2026),
(8, 'ПОРТ-24', 3, 2024),
(9, 'ПОРТ-26', 1, 2026),
(10, 'ДПИ-25', 2, 2025),
(11, 'РМ-25', 2, 2025),
(12, 'РМ-26', 1, 2026),
(13, 'ЭРМА-24', 3, 2024),
(14, 'ДО-24', 3, 2024),
(15, 'МСР-26', 1, 2026),
(16, 'МСР-25', 2, 2025),
(17, 'МСР-24', 3, 2024),
(18, 'МШИ-26', 1, 2026),
(19, 'МШИ-25', 2, 2025);

-- Связь групп и дисциплин
INSERT INTO group_disciplines (group_id, discipline_id, teacher_id) VALUES
(1, 1, 1), (1, 2, 2), (1, 3, 4), (1, 4, 3), (1, 5, 3),
(2, 1, 1), (2, 2, 2), (2, 3, 1);

-- Расписание занятий для группы ИСП-23 (чётная и нечётная недели)
INSERT INTO schedule (id, group_id, discipline_id, teacher_id, day_of_week, pair_number, is_even_week, audience, start_time, end_time) VALUES
(1, 1, 1, 1, 1, 1, FALSE, 'Ауд. 402 (К-1)', '09:00', '10:30'),
(2, 1, 2, 2, 1, 2, FALSE, 'Лаб. 305 (К-1)', '10:45', '12:15'),
(3, 1, 3, 4, 2, 1, FALSE, 'Ауд. 210 (К-2)', '09:00', '10:30'),
(4, 1, 4, 3, 3, 2, FALSE, 'Ауд. 408 (К-1)', '10:45', '12:15'),
(5, 1, 1, 1, 4, 3, FALSE, 'Лаб. 404 (К-1)', '13:00', '14:30'),
(6, 1, 5, 3, 5, 1, FALSE, 'Ауд. 112 (К-1)', '09:00', '10:30'),
(7, 1, 2, 2, 1, 1, TRUE,  'Лаб. 305 (К-1)', '09:00', '10:30'),
(8, 1, 1, 1, 2, 2, TRUE,  'Лаб. 404 (К-1)', '10:45', '12:15'),
(9, 1, 3, 4, 3, 1, TRUE,  'Ауд. 210 (К-2)', '09:00', '10:30'),
(10, 1, 4, 3, 4, 2, TRUE, 'Ауд. 408 (К-1)', '10:45', '12:15'),
(11, 1, 5, 3, 5, 2, TRUE, 'Лаб. 115 (К-1)', '10:45', '12:15');

-- Администратор системы
INSERT INTO admins (id, login, password_hash, full_name, is_super_admin, last_login_at) VALUES
(1, 'admin', '8c6976e5b5410415bde908bd4dee15dfb167a9c873fc4bb8a81f6f2ab448a918', 'Администратор Учебного Отдела', TRUE, CURRENT_TIMESTAMP);

-- Пользователи (Студенты)
INSERT INTO users (id, last_name, first_name, patronymic, email, phone, password_hash, group_id, registration_date, is_admin, is_blocked, block_reason) VALUES
(1, 'Иванов', 'Захар', 'Александрович', 'zahar@student.edu.ru', '+7 (999) 123-45-67', 'ef92b778bafe771e89245b89ecbc08a44a4e166c06659911881f383d4473e94f', 1, '2026-09-01 10:00:00', FALSE, FALSE, NULL),
(2, 'Смирнова', 'Анна', 'Дмитриевна', 'smirnova.ad@student.edu.ru', '+7 (999) 234-56-78', 'ef92b778bafe771e89245b89ecbc08a44a4e166c06659911881f383d4473e94f', 1, '2026-09-02 14:20:00', FALSE, FALSE, NULL),
(3, 'Кузнецов', 'Максим', 'Олегович', 'kuznetsov.mo@student.edu.ru', '+7 (999) 345-67-89', 'ef92b778bafe771e89245b89ecbc08a44a4e166c06659911881f383d4473e94f', 2, '2026-09-05 09:15:00', FALSE, TRUE, 'Нарушение регламента использования портала');

-- Учебные задачи студента Захара
INSERT INTO tasks (id, title, description, deadline, priority_id, status_id, type_id, discipline_id, user_id, schedule_id, created_at, completed_at) VALUES
(1, 'Защита ER-диаграммы и DDL-скрипта БД PostgreSQL', 'Подготовить физическую схему из 15 таблиц, индексы и заполнить справочники.', '2026-10-20 12:15:00', 4, 2, 1, 2, 1, 2, '2026-10-05 11:00:00', NULL),
(2, 'Разработка контроллеров ASP.NET Core и авторизации', 'Реализовать регистрацию со сверкой паролей, вход студента и панель администратора.', '2026-10-22 14:30:00', 4, 2, 3, 1, 1, 5, '2026-10-07 15:30:00', NULL),
(3, 'Построение диаграмм Use Case и блок-схем процессов', 'Сформировать диаграмму прецедентов и 3 блок-схемы (регистрация, авторизация, основной процесс).', '2026-10-16 10:30:00', 3, 3, 2, 3, 1, 3, '2026-10-03 09:00:00', '2026-10-15 18:40:00'),
(4, 'Лабораторная работа №4: Сбалансированные деревья поиска', 'Реализовать АВЛ-дерево и красно-чёрное дерево на C#, провести замеры времени.', '2026-10-25 10:45:00', 2, 1, 1, 4, 1, 4, '2026-10-10 12:00:00', NULL),
(5, 'Анализ трафика протокола HTTPS в Wireshark', 'Описать процедуру TLS Handshake и предоставить отчёт по захваченным пакетам.', '2026-10-14 09:00:00', 3, 4, 1, 5, 1, 6, '2026-10-01 16:00:00', NULL),
(6, 'Реализация модуля экспорта отчётов в XLSX и PDF', 'Подключить генерацию табличного отчёта успеваемости и логирование в report_exports.', '2026-10-28 13:00:00', 3, 1, 3, 1, 1, 5, '2026-10-12 10:20:00', NULL);

-- Напоминания для активных задач
INSERT INTO reminders (id, task_id, user_id, reminder_type_id, scheduled_at, sent_at, is_sent) VALUES
(1, 1, 1, 1, '2026-10-17 12:15:00', '2026-10-17 12:15:05', TRUE),
(2, 1, 1, 2, '2026-10-19 12:15:00', NULL, FALSE),
(3, 1, 1, 3, '2026-10-20 09:15:00', NULL, FALSE),
(4, 2, 1, 1, '2026-10-19 14:30:00', NULL, FALSE),
(5, 2, 1, 2, '2026-10-21 14:30:00', NULL, FALSE);

-- История выгруженных отчётов
INSERT INTO report_exports (id, user_id, format, period_from, period_to, created_at) VALUES
(1, 1, 'XLSX', '2026-09-01', '2026-10-15', '2026-10-15 19:00:00'),
(2, 1, 'PDF',  '2026-10-01', '2026-10-31', '2026-10-17 14:10:00');

SELECT setval('priorities_id_seq', (SELECT MAX(id) FROM priorities));
SELECT setval('statuses_id_seq', (SELECT MAX(id) FROM statuses));
SELECT setval('task_types_id_seq', (SELECT MAX(id) FROM task_types));
SELECT setval('disciplines_id_seq', (SELECT MAX(id) FROM disciplines));
SELECT setval('teachers_id_seq', (SELECT MAX(id) FROM teachers));
SELECT setval('groups_id_seq', (SELECT MAX(id) FROM groups));
SELECT setval('schedule_id_seq', (SELECT MAX(id) FROM schedule));
SELECT setval('users_id_seq', (SELECT MAX(id) FROM users));
SELECT setval('admins_id_seq', (SELECT MAX(id) FROM admins));
SELECT setval('tasks_id_seq', (SELECT MAX(id) FROM tasks));
SELECT setval('reminder_types_id_seq', (SELECT MAX(id) FROM reminder_types));
SELECT setval('reminders_id_seq', (SELECT MAX(id) FROM reminders));
SELECT setval('report_exports_id_seq', (SELECT MAX(id) FROM report_exports));
