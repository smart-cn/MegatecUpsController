
# Megatec UPS Controller
Приложение для контроля вашего ИБП на основе модифицированного протокола Megatec (Ippon и некоторые другие), подключаемого к компьютеру по USB, а также для управления питанием компьютера.
В данной версии - совместим как минимум с PowerMust 636 UPS (для него, собственно и создавался форк оригинального репо, поскольку используемый данным ИБП протокол отличается от оригинального Megatec).
Так-же рекомендуется, при необходимости, попробовать оригинальную программу, если с данной версией ваш ИБП не определился но теоретичесски должен поддерживать протокол Megatec.

![Главное окно](https://raw.githubusercontent.com/smart-cn/MegatecUpsController/master/img/main.png "Главное окно")

## Требования к ОС
- Windows 7 и выше
- NET Framework 4.7.2 и выше

## Как начать это использовать?
- Последнюю версию в виде zip-архива можно скачать по ссылке [https://github.com/smart-cn/MegatecUpsController/releases/latest](https://github.com/smart-cn/MegatecUpsController/releases/latest "ЗДЕСЬ")
- Эволюция (она же История версий) описана в разделе релизов

## Что оно умеет
+ Красивое отображение получаемых от ИБП данных:
	+ входное и выходное напряжение
	+ напряжение и заряд батареи
	+ частота
	+ ток нагрузки ИБП (в процентах, амперах, ваттах, вольт-амперах)
	+ температура ИБП (хотя в большинстве случаев она всегда 0°)
	+ состояние AVR
+ Масштабируемый интерфейс (хоть на весь экран)
+ Возможность включать-выключать надоедливую пищалку ИБП
+ График входного и выходного напряжения
+ Выключение или гибернация компьютера при разряде батареи
+ Настройка напряжений батареи и мощности под ваш ИБП
+ Запись событий в текстовый файл
+ Отправка команды по SSH на удалённую Linux-машину, при завершении работы/гибернации

![Окно настроек](https://raw.githubusercontent.com/smart-cn/MegatecUpsController/master/img/settings.png "Окно настроек")

## На чём же всё это построено
- C# WPF в Microsoft Visual Studio 2019
- внешний вид интерфейса - по мотивам программы [Energy Controller 2](https://sites.google.com/site/ibakhlab/News/energycontroller20582332200sp5 "Energy Controller 2")
- библиотека UsbLibrary.dll - доработанная от [adelectronics.ru](https://adelectronics.ru/2016/11/22/usblibrary-c-usb-hid-library/ "adelectronics.ru") (так как [изначальная](https://www.codeproject.com/Articles/18099/A-USB-HID-Component-for-C "изначальная") попила кровушки и блистала багами вроде невозможности видеть потерю связи с USB)
- описание Megatec-протокола - от [networkupstools.org](https://networkupstools.org/protocols/megatec.html "networkupstools.org")
- немногочисленные иконки - [FontAwesome](https://fontawesome.com/ "FontAwesome") (хотя тащить всю библиотеку ради пары иконок такое себе. Зато они масштабируются)
- вывод графика - библиотека [InteractiveDataDisplay.WPF](https://github.com/microsoft/InteractiveDataDisplay.WPF "InteractiveDataDisplay.WPF")
- работа с SSH - библиотека [SSH.NET](https://github.com/sshnet/SSH.NET "SSH.NET")

## Другое
- программа бесплатная
- можно распространять и изменять
- если распространили или изменили - укажите что поменяли и первоначального автора
- программа поставляется как есть, автор не даёт никаких гарантий безошибочной работы, не оказывает техническую поддержку, не несёт никакой ответственности за потерянные данные, сгоревшие компы, взорванные ИБП и тому подобное.
