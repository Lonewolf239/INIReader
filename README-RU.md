# NeoIni

**NeoIni** — это полнофункциональная библиотека на C# для работы с INI-файлами. Она обеспечивает безопасные, потокобезопасные операции по созданию, чтению и изменению файлов с встроенной проверкой контрольных сумм и опциональным шифрованием AES.

## Установка

```bash
dotnet add package NeoIni
```

**Пакет:** [nuget.org/packages/NeoIni](https://www.nuget.org/packages/NeoIni)  
**Версия:** `1.5.6` | **.NET 6+**
**Разработчик:** [Lonewolf239](https://github.com/Lonewolf239)

## Возможности

- Чтение и парсинг типизированных значений (bool, int, float, double, string и др.) из INI-файлов
- Автоматическое определение типов и парсинг с возвратом значений по умолчанию
- Потокобезопасные операции с использованием блокировок
- Автоматическое сохранение файла и создание секций/ключей (настраивается через `AutoSave`, `AutoAdd`, `AutoBackup` и др.)
- Встроенная проверка контрольных сумм (SHA256) для обеспечения целостности данных
- Опциональное шифрование AES-256 с генерацией пароля на основе данных пользователя
- Поддержка стандартного формата INI с секциями и парами ключ-значение
- Полная поддержка асинхронных методов для всех операций
- Автоматическое создание директорий и инициализация файла

## Функции безопасности

- **Контрольная сумма**: Проверка SHA256 обнаруживает вмешательство или повреждение
- **AES-256**: Ключ шифрования генерируется из данных окружения пользователя с вектором инициализации IV
- **Потокобезопасность**: Все операции защищены блокировками `lock`
- Пароль генерируется из `Environment.UserName`, `MachineName`, `UserDomainName`

## Целевая платформа

.NET 6+

## Использование

### Создание экземпляра NeoIni

```cs
NeoIniReader reader = new("config.ini");                                          // Обычное использование
NeoIniReader encryptedReader = new("config.ini", true);                           // С автошифрованием
NeoIniReader encryptedCustomReader = new("config.ini", "MySecretPas123");         // С пользовательским паролем
```

### Чтение значений

```cs
string text = reader.GetValue<string>("Section1", "Key1", "default");
int number = reader.GetValue<int>("Section1", "Number", 0);
bool flag = reader.GetValue<bool>("Section1", "Enabled", false);
double value = reader.GetValue<double>("Section1", "Value", 0.0);
```

### Запись значений

```cs
reader.SetKey("Section1", "Key1", "Value1");
reader.SetKey("Section1", "Number", 42);
reader.SetKey("Section1", "Enabled", true);
```

## Пример

```cs
using NeoIniReader reader = new("config.ini");

reader.SetKey("Database", "Host", "localhost");
reader.SetKey("Database", "Port", 5432);
reader.SetKey("Settings", "AutoSave", true);

string host = reader.GetValue<string>("Database", "Host", "127.0.0.1");
int port = reader.GetValue<int>("Database", "Port", 3306);

Console.WriteLine($"База данных: {host}:{port}");
```

## Управление секциями и ключами

```cs
bool exists = reader.SectionExists("Section1");
bool keyExists = reader.KeyExists("Section1", "Key1");

reader.AddSection("NewSection");
reader.RemoveKey("Section1", "Key1");
reader.RemoveSection("Section1");

string[] sections = reader.GetAllSections();
string[] keys = reader.GetAllKeys("Section1");
```

## Операции с файлами

```cs
reader.SaveFile();                    // Ручное сохранение
await reader.SaveFileAsync();         // Асинхронное сохранение

reader.ReloadFromFile();              // Перезагрузка с диска
await reader.ReloadFromFileAsync();   // Асинхронная перезагрузка с диска

reader.DeleteFile();                  // Удаление только файла
reader.DeleteFileWithData();          // Удаление файла + очистка данных
```

## Справочник API

| Метод | Описание | Асинхронная версия |
|-------|----------|--------------------|
| `GetValue<T>` | Чтение типизированного значения с возвратом по умолчанию | `GetValueAsync<T>` |
| `SetKey<T>` | Установка/создание пары ключ-значение | `SetKeyAsync<T>` |
| `AddSection` | Создание секции при отсутствии | `AddSectionAsync` |
| `AddKeyInSection<T>` | Добавление уникальной пары ключ-значение | `AddKeyInSectionAsync<T>` |
| `RemoveKey` | Удаление конкретного ключа | `RemoveKeyAsync` |
| `RemoveSection` | Удаление целой секции | `RemoveSectionAsync` |
| `GetAllSections` | Список всех секций | `GetAllSectionsAsync` |
| `GetAllKeys` | Список ключей секции | `GetAllKeysAsync` |
| `GetSection` | Получение всех пар ключ-значение секции | `GetSectionAsync` |
| `FindKeyInAllSections` | Поиск ключа по всем секциям | `FindKeyInAllSectionsAsync` |
| `ClearSection` | Удаление всех ключей из секции | `ClearSectionAsync` |
| `RenameKey` | Переименование ключа в секции | `RenameKeyAsync` |
| `RenameSection` | Переименование секции | `RenameSectionAsync` |
| `Search` | Поиск ключей/значений по шаблону | `SearchAsync` |
| `SectionExists` | Проверка существования секции | `SectionExistsAsync` |
| `KeyExists` | Проверка существования ключа в секции | `KeyExistsAsync` |
| `SaveFile` | Сохранение данных в файл | `SaveFileAsync` |
| `ReloadFromFile` | Перезагрузка данных из файла | `ReloadFromFileAsync` |
| `DeleteFile` | Удаление файла с диска | `DeleteFileAsync` |
| `DeleteFileWithData` | Удаление файла и очистка данных | `DeleteFileWithDataAsync` |
| `GetEncryptionPassword` | Получение пароля шифрования (или статуса) | `GetEncryptionPasswordAsync` |

| Опция | Описание | Значение по умолчанию |
|-------|----------|----------------------|
| `AutoSave` | Автоматическое сохранение изменений на диск после модификаций | `true` |
| `UseAutoSaveInterval` | При включенном AutoSave определяет, сохранять ли через регулярные интервалы вместо каждого изменения | `true` |
| `AutoSaveInterval` | Количество операций между автоматическими сохранениями при включенных AutoSave и UseAutoSaveInterval | `5` |
| `AutoBackup` | Создание резервных копий (.backup) при операциях сохранения | `true` |
| `AutoAdd` | Автоматическое создание отсутствующих секций/ключей со значениями по умолчанию при чтении через `GetValue<T>` | `true` |
| `UseChecksum` | Вычисление и проверка контрольных сумм при загрузке/сохранении для обнаружения повреждений | `true` |

| Событие | Описание |
|---------|----------|
| `OnSave` | Вызывается перед сохранением файла на диск |
| `OnLoad` | Вызывается после успешной загрузки данных из файла или перезагрузки |
| `OnKeyChanged` | Вызывается при изменении значения существующего ключа в секции |
| `OnKeyAdded` | Вызывается при добавлении нового ключа в секцию |
| `OnKeyRemoved` | Вызывается при удалении ключа из секции |
| `OnSectionChanged` | Вызывается при любом изменении секции (изменение/добавление/удаление ключей) |
| `OnSectionAdded` | Вызывается при добавлении новой секции |
| `OnSectionRemoved` | Вызывается при удалении секции |
| `OnDataCleared` | Вызывается при полной очистке данных |
| `OnAutoSave` | Вызывается перед автоматическим сохранением |
| `OnError` | Вызывается при ошибках (парсинг, сохранение, чтение файла и т.д.) |

## Философия

**Черный ящик**: Взаимодействуйте только через публичный API — внутренности полностью абстрагированы
