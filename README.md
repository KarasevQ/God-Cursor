## ENG

# God Cursor
Mod for Rimworld 1.6


## What does this mod do?
It adds the ability to turn the cursor—which previously could only select pawns and control them through menus—into… a God Cursor! Now your cursor can:

* Grab pawns with the cursor.
* Drag pawns anywhere on the map.
* Throw them or simply place them on the ground.
* Harm them through throwing and rough landings (disabled by default).
* Pet.
* Poke.
* Throw anyone off the map (toggleable feature).

And that's not all: if you haven't disabled the "Fourth Wall" and "Pawns talk to themselves and to you" settings, pawns will…:

* Build their attitude toward you, from Indifference to Horror or Worship.
* Comment on the cursor, on you, or just say something to themselves from time to time. (Over 200 lines available!)
* Be able to realize their nature and see your UI.
* Tremble if they are afraid of you.
* Panic and try to run away from you (if "Pawns can panic" is enabled).

## Controls:

F8 – Open the mod menu and enable/disable the God Cursor. (Can be changed in controls)

| Action | Result |
|---|---|
| LMB + drag over a pawn | pick up and drag (a grabbed panicking pawn calms down) |
| Release slowly | place carefully |
| Release while moving (swing) | throw; force depends on mouse speed |
| Shift + LMB | poke (scares, pawn jumps) |
| Ctrl + LMB | pet (improves attitude and calms a frightened pawn) |
| Simple click without dragging | normal pawn selection |

## Settings window:
* **Throws deal damage** / **Damage can be lethal** / damage multiplier / throw force.
* **Do not let pawns leave the map** (enabled by default). Enabled: thrown pawns bounce off the edge, and panicking pawns are calmed before they can reach the edge. Disabled: a pawn thrown over the edge truly leaves the map (can be lost forever).
* **Fourth Wall** — lines, attitude toward the cursor, awareness.
* **Pawns talk to themselves and to you** — rare spontaneous lines from aware pawns.
* **Pawns can panic** (disabled by default). Panic caused by the cursor is removed by: grabbing the pawn, petting it (Ctrl+LMB), the **“Calm all frightened pawns”** button in the panel, automatically after a set number of seconds, and automatically at the map edge (if map protection is enabled).
* **Hints** — four independent toggles: the “God Cursor” label near the cursor (disabled by default), pawn info on hover, a hint while holding a pawn (disabled by default), and help in the panel.

## Flexible dialogue system without code edits
Pawns speak to the “cursor,” to the “player,” and to “themselves”: reactions to grabbing/throwing/placing/hitting/poking/petting, hover lines (based on attitude), meta-lines based on awareness level, spontaneous lines, panic, calming. The number of variants in each pool is determined automatically by the keys `GC_B_<POOL>_<N>` in `Languages/*/Keyed/GodCursor.xml` — to add a line, just add the next number to both files; no code changes needed.

## AI usage:

I am not hiding at all that I used AI while creating the mod. However, the mod was **tested, edited, and compiled by me personally**. I play with this mod myself. I had long dreamed of making such a mod and looked for something similar from other modders, studied it, but I didn't know many of the pitfalls and workarounds that AI figured out.

## FAQ:

* I found a bug or an error! – Please write about it, and I will try to fix the problem.
* Does this work only on humanoids? – No, it works on any Pawn, including animals. But at the time of writing I haven't yet tested how it works with the Anomaly DLC on Anomalies; it probably should work the same way on all anomalies that are alive (well… or conditionally alive).
* A pawn doesn't fly off the map edge. – Check whether the “Do not let pawns leave the map” setting is disabled.
* I can't deal damage – Same as #3: check the first two settings, “Throws deal damage” and “Damage can be lethal,” and enable the first or both depending on what you want from it. You can also edit the damage amount with the slider.
* I don't want this reaction system – You can disable it completely and just drag pawns. Just disable the “Fourth Wall” and “Pawns talk to themselves and to you” settings.
   Will the mod be available for version 1.7? - I’m not sure. I use the GOG version, and I haven’t updated to 1.7 because I’m running Windows 7 with 4 GB of RAM. I might release a version for 1.7, but I won’t be able to test it myself—though the developers did promise that 1.7 would be compatible with 1.6 mods, presumably without requiring any changes. You could try changing 1.6 to "1.7" in the About.xml file and see if it works.

## Credits:

Well… Thanks Claude?

## License: **MIT**
(What the hell does that mean??? – I just… don't care. Do whatever you want with it, just credit me. Thanks.)


----------------------------------------------

## RUS

# God Cursor

##  Что делает этот мод?
Он добавляет возможность превратить курсор, который ранее был способен лишь выбирать пешек и управлять ими через меню в… Бога-курсора! Теперь ваш курсор будет способен:

* Брать пешек курсором. 
* Таскать пешек в любое место на карте.
* Кидать их или просто ставить на землю.
* Причинять им вред через бросок и грубое приземление (По умолчанию отключено).
* Гладить.
* Тыкать.
* Выбрасывать кого угодно за пределы карты (Отключаемая функция).

И это не все, если вы не выключили параметр «Четвертая стена» и «Пешки говорят с собой и с вами», то пешки…:

* Будут выстраивать своё отношение к вам, от Безразличия до Ужаса или Боготворения. 
* Комментировать курсор, вас, или просто говорить что-то себе время от времени. (Доступно более 200 фраз!)
* Будут иметь возможность осознать свою природу и увидеть ваш интерфейс. 
* Трястись, если они боятся вас.
* Впадать в панику и пытаться убежать от вас (Если включен параметр «Пешки могут впадать в панику»)

##  Управление:

F8 – Вызов меню мода и включение/выключение курсора-бога. (Можно изменить в управлении)

| Действие | Результат |
|---|---|
| ЛКМ + тянуть по пешке | поднять и таскать (схваченная паникующая пешка успокаивается) |
| Отпустить медленно | аккуратно поставить |
| Отпустить на ходу (взмах) | бросок; сила зависит от скорости мыши |
| Shift + ЛКМ | ткнуть (пугает, пешка подпрыгивает) |
| Ctrl + ЛКМ | погладить (улучшает отношение и успокаивает испуганную пешку) |
| Простой клик без перетаскивания | обычный выбор пешки |

## Окно настроек:
* **Броски наносят урон** / **Урон может быть смертельным** / множитель урона / сила броска.
* **Не выпускать пешек за пределы карты** (включено по умолчанию). Включено: брошенные пешки отскакивают от края,
  а паникующих успокаивает, не дав добежать до края. Выключено: пешка, брошенная за край, по-настоящему покидает карту
  (можно потерять навсегда).
* **Четвёртая стена** — реплики, отношение к курсору, осознание.
* **Пешки говорят сами с собой и с вами** — редкие самостоятельные реплики осознавших пешек.
* **Пешки могут паниковать** (выключено по умолчанию). Паника, вызванная курсором, снимается:
  схватить пешку, погладить её (Ctrl+ЛКМ), кнопка **«Успокоить всех напуганных пешек»** в панели,
  автоматически через заданное число секунд, и автоматически у края карты (если включена защита карты).
* **Подсказки** — четыре независимых переключателя: надпись «Курсор-бог» у курсора (по умолчанию выкл.),
  информация о пешке при наведении, подсказка при удержании пешки (по умолчанию выкл.), справка в панели.

## Гибкая система реплик без правки кода
Пешки говорят «курсору», «игроку» и «себе»: реакции на хват/бросок/посадку/удар/тычок/поглаживание,
реплики при наведении (по отношению), мета-реплики по уровню осознания, самостоятельные реплики, паника, успокоение.
Количество вариантов в каждом пуле определяется автоматически по ключам `GC_B_<ПУЛ>_<N>` в
`Languages/*/Keyed/GodCursor.xml` — чтобы добавить фразу, просто допишите следующий номер в оба файла, код менять не нужно.

##  Использование ИИ:

Я абсолютно не скрываю что использовал ИИ при создании мода. Однако мод **протестирован, отредактирован и скомпилирован мной лично**. Я играю с этим модом сам. Я давно мечтал сделать такой мод и искал что-то похожее у других моддеров, изучал, но я не знал многих подвохов и костылей, которые вычислил ИИ. 

## Вопрос-Ответ:

* Я нашел баг или ошибку! – Пожалуйста напишите об этом, я постараюсь решить эту проблему. 
* Это работает только на гуманоидах? – Нет, это работает на любых пешках (Pawn) в том числе на животных. Но я на момент написания ещё не протестировал как это работает с DLC Anomaly на Аномалиях, вероятно оно должно работать точно так же на всех аномалиях, которые являются живыми (ну… или условно живыми)
* Пешка не вылетает за границы карты. - Проверьте выключен ли параметр «Не выпускать пешек за пределы карты».
* Я не могу нанести урон – Аналогично третьему, проверьте первые два параметра «Броски наносят урон» и «Урон может быть смертельным», включите первый или оба параметра в зависимости от того что вы хотите от этого получить. Вы так же можете отредактировать силу урона через ползунок.
* Я не хочу эту систему реакций – Вы можете её полностью отключить и просто таскать пешек. Просто отключите параметры «Четвертая стена» и «Пешки говорят с собой и с вами».
* Будет ли мод доступен для 1.7? - Я не уверен. У меня GOG и я не обновился до 1.7 из-за своего Win 7 и 4 ГБ ОЗУ. Возможно я выложу это для 1.7, но тестировать я это уже не смогу, хотя разработчики обещали совместимость 1.7 с модами из 1.6, вроде как без изменений. Вы можете сменить 1.6 в About.xml на 1.7 и проверить работоспособность. 

##  Кредиты:

Ну… Спасибо Claude?

## Лицензия: **MIT**
(Что это черт подери значит??? – Мне просто... Все равно. Делайте с этим что хотите, просто укажите меня в кредитах. Спасибо.)
