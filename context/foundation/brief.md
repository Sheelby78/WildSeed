# Wild Seed — Product Requirements Document

## 1. Overview

**Wild Seed** to interaktywna symulacja ekosystemu 2D, w której autonomiczne organizmy żyją, zdobywają zasoby, polują, uciekają, rozmnażają się i umierają.

Kluczowym elementem projektu jest **emergent behaviour** — zachowania populacji i kierunek ewolucji nie są z góry zaprogramowane.

Każdy organizm posiada dziedziczny genom. Potomstwo dziedziczy cechy rodziców z niewielkimi mutacjami. Presja środowiska powoduje, że po wielu pokoleniach populacje zaczynają zmieniać swoje właściwości.

Użytkownik nie steruje zwierzętami. Jest obserwatorem i eksperymentatorem mogącym analizować oraz modyfikować świat.

---

# 2. Product Vision

Aplikacja powinna sprawiać wrażenie obserwowania **żywego, autonomicznego świata**.

Najważniejszym doświadczeniem użytkownika ma być:

> „Nie zaprogramowałem tego konkretnego zachowania. Ono powstało samo z interakcji prostych zasad.”

Symulacja powinna umożliwiać obserwowanie zależności takich jak:

* wzrost populacji roślinożerców,
* wzrost populacji drapieżników w odpowiedzi,
* spadek liczby ofiar,
* głód wśród drapieżników,
* spadek ich populacji,
* ponowny wzrost populacji roślinożerców,
* migracje spowodowane brakiem zasobów,
* naturalna selekcja określonych cech,
* lokalne wymieranie populacji.

Docelowo system powinien umożliwiać powstawanie zjawisk, których twórca symulacji nie zaprogramował bezpośrednio.

---

# 3. Core Principles

### Emergence over scripting

Nie tworzymy scenariuszy typu:

`Year 200 → drought → species dies`.

Tworzymy systemy i reguły, których interakcje prowadzą do konsekwencji.

### Simulation first

Najważniejszym komponentem projektu jest silnik symulacji.

UI jest sposobem obserwowania jego działania.

### Deterministic simulation

Ten sam:

`Seed + Configuration`

powinien prowadzić do tego samego wyniku symulacji.

Pozwala to na replay, debugowanie i porównywanie eksperymentów.

### Performance

Symulacja musi działać niezależnie od renderowania.

Docelowo powinna być możliwa symulacja tysięcy organizmów znacznie szybciej niż realtime.

---

# 4. Technology

## Backend

**.NET / ASP.NET Core**

Odpowiedzialności:

* Simulation Engine
* World Generation
* Creature Behaviour
* Genetics
* Reproduction
* Combat
* Resources
* Statistics
* Snapshots
* Events

## Frontend

**React + TypeScript**

UI aplikacji, konfiguracja świata, inspekcja organizmów i analiza danych.

## World Rendering

**PixiJS / WebGL**

Odpowiedzialny za wydajne renderowanie:

* mapy,
* organizmów,
* roślinności,
* wody,
* efektów,
* overlayów.

React nie powinien renderować każdego organizmu jako osobnego komponentu DOM.

## Communication

**SignalR**

Streaming aktualnego stanu symulacji oraz wydarzeń.

---

# 5. World

Świat reprezentowany jest jako mapa 2D.

Pierwsza wersja może wykorzystywać prostą mapę generowaną proceduralnie.

Każdy obszar posiada właściwości środowiskowe.

Przykład:

```text
Terrain
Temperature
Water
Vegetation
Food availability
```

Podstawowe typy terenu:

* grassland,
* forest,
* water,
* barren land.

W MVP wystarczy:

**land + water + vegetation.**

---

# 6. Plants

Roślinność jest podstawowym źródłem energii ekosystemu.

Każdy obszar posiada ilość dostępnej biomasy.

Roślinność:

* rośnie z czasem,
* jest konsumowana przez roślinożerców,
* posiada maksymalną lokalną pojemność,
* regeneruje się zależnie od warunków środowiska.

Przykład:

```text
Grass

Current biomass: 72
Maximum biomass: 100
Growth rate: 1.2/day
```

Nie ma potrzeby symulowania każdej rośliny jako osobnego obiektu.

---

# 7. Creatures

Każdy organizm jest autonomiczną jednostką symulacji.

Posiada między innymi:

```text
Position
Age
Health
Energy
Hunger
Thirst
Sex
Genome
CurrentAction
```

Organizm może wykonywać akcje:

```text
Idle
Explore
MoveToFood
MoveToWater
Eat
Drink
Flee
Hunt
Attack
Mate
Rest
```

---

# 8. Needs

Podstawowe potrzeby organizmu:

### Hunger

Rośnie wraz z metabolizmem.

Brak jedzenia powoduje utratę zdrowia i śmierć.

### Thirst

Rośnie szybciej niż głód.

Organizm musi znajdować źródła wody.

### Energy

Ruch, walka i polowanie zużywają energię.

Odpoczynek pozwala ją odzyskać.

### Reproduction

Organizm może szukać partnera po spełnieniu odpowiednich warunków.

---

# 9. Genome

Każdy organizm posiada genom definiujący jego właściwości.

Pierwsza wersja:

```text
Size
Speed
Strength
VisionRange
Metabolism
Aggression
Fear
Fertility
Lifespan
```

Cechy powinny mieć trade-offy.

Przykład:

**Speed ↑**

powoduje:

**Energy consumption ↑**

**Size ↑**

powoduje:

```text
Strength ↑
Energy requirement ↑
Speed ↓
```

Dzięki temu nie powinien istnieć jeden oczywisty „najlepszy genom”.

---

# 10. Diet

Na początku istnieją dwa archetypy:

### Herbivore

Może konsumować vegetation.

### Carnivore

Może konsumować inne organizmy.

Docelowo dieta może również być częścią genomu.

---

# 11. Behaviour

Organizmy nie posiadają z góry zaprogramowanego planu.

Każdego ticka system ocenia możliwe zachowania.

Przykład:

```text
Thirst = 90
Hunger = 40
Predator nearby = false

→ SeekWater
```

Inny przypadek:

```text
Thirst = 90
Predator nearby = true

→ Flee
```

Decyzja może być oparta na utility scoring.

Przykładowo:

```text
Eat       0.62
Drink     0.91
Flee      0.97
Mate      0.12
Explore   0.20
```

Najwyżej oceniona akcja zostaje wykonana.

Pozwala to później rozwijać zachowania bez tworzenia ogromnych drzew `if/else`.

---

# 12. Perception

Organizmy nie posiadają globalnej wiedzy o świecie.

`VisionRange` określa obszar, który mogą obserwować.

Organizm może wykrywać:

* jedzenie,
* wodę,
* partnerów,
* drapieżniki,
* potencjalne ofiary.

To wymusza eksplorację i pozwala na naturalne powstawanie zachowań.

---

# 13. Hunting

Drapieżnik musi:

1. wykryć ofiarę,
2. zdecydować o rozpoczęciu polowania,
3. dogonić ją,
4. zaatakować,
5. zabić,
6. zjeść.

Ofiara po wykryciu zagrożenia próbuje uciekać.

Powodzenie polowania zależy m.in. od:

```text
Speed
Vision
Strength
Size
Energy
```

---

# 14. Combat

Organizmy mogą walczyć.

W MVP walka może być prostym systemem opartym o:

```text
Damage =
Strength × Size × modifier
```

Organizm może zdecydować o ucieczce zależnie od:

```text
Fear
Health
EnemyStrength
Energy
```

---

# 15. Reproduction

Organizmy mogą rozmnażać się po osiągnięciu:

* minimalnego wieku,
* odpowiedniego poziomu energii,
* odpowiedniego stanu zdrowia.

Potomstwo otrzymuje genom będący kombinacją genomów rodziców.

Przykład:

```text
Parent A Speed = 7.4
Parent B Speed = 8.0

Child Speed ≈ 7.7
```

Następnie aplikowana jest mutacja:

```text
7.7 → 7.91
```

---

# 16. Mutation

Każda cecha posiada niewielką szansę mutacji.

Konfiguracja eksperymentu:

```text
Mutation probability: 5%
Mutation strength: 10%
```

Mutacje mogą być:

* pozytywne,
* negatywne,
* neutralne w danym środowisku.

System nie określa, czy mutacja jest „dobra”.

Określa to przeżywalność organizmu.

---

# 17. Natural Selection

Nie istnieje osobny algorytm:

```text
NaturalSelectionSystem
```

Selekcja powinna być **konsekwencją działania świata**.

Jeżeli szybsze zwierzę:

* częściej ucieka,
* dłużej żyje,
* częściej się rozmnaża,

jego geny automatycznie zaczynają dominować w populacji.

---

# 18. Species

## MVP

Dwa bazowe gatunki:

```text
Herbivore
Carnivore
```

## Future

System automatycznej specjacji.

Jeżeli część populacji genetycznie oddali się wystarczająco mocno:

```text
Species 4
   │
   ├── Species 4
   │
   └── Species 12
```

powstaje nowy gatunek.

---

# 19. Death

Organizm może umrzeć z powodu:

```text
Starvation
Dehydration
Combat
Predation
OldAge
```

Śmierć generuje event.

Docelowo ciało może również przez pewien czas istnieć jako źródło pożywienia.

---

# 20. Simulation Speed

Użytkownik może kontrolować prędkość:

```text
PAUSE
x1
x5
x20
x100
MAX
```

`MAX` oznacza brak synchronizacji z czasem rzeczywistym.

Silnik wykonuje symulację tak szybko, jak pozwala sprzęt.

Rendering może być wtedy ograniczony lub całkowicie wyłączony.

---

# 21. Main UI

Głównym ekranem jest mapa świata.

```text
┌──────────────────────────────────────────────────┐
│ Wild Seed              Year 184     ▶ x20        │
├───────────────────────────────────┬──────────────┤
│                                   │ POPULATION   │
│        🌲                         │              │
│            🦌 🦌                  │ Herb.  1842  │
│      🦌                           │ Carn.   193  │
│                     🐺            │              │
│        ~~~~~~~~~~~~               │ Births   82  │
│        ~   WATER  ~               │ Deaths   71  │
│        ~~~~~~~~~~~~               │              │
│                                   │              │
├───────────────────────────────────┴──────────────┤
│ Population ━━━━━╮╭━━━━━━━━━━━━━━━━━━━━━━━━━━━━ │
│                 ╰╯                              │
└──────────────────────────────────────────────────┘
```

---

# 22. Creature Inspector

Kliknięcie organizmu zatrzymuje/podświetla go.

Panel:

```text
Creature #18492

Species
Herbivore

Age
4.2 years

Health
████████░░ 81%

Energy
██████░░░░ 63%

Hunger
████████░░ 78%

Current action
FLEEING

Genome

Speed        8.42
Size         3.21
Strength     2.84
Vision       11.7
Metabolism   4.12
Fear         7.31
```

Dodatkowo:

```text
Mother
Father
Children
```

---

# 23. Population Analytics

System zapisuje statystyki populacji.

Minimum:

```text
Population
Births
Deaths
Average lifespan

Average Speed
Average Size
Average Strength
Average Vision
```

Użytkownik może obserwować zmianę cech w czasie.

To jest kluczowe dla pokazania ewolucji.

---

# 24. Event System

Silnik generuje znaczące wydarzenia.

Przykłady:

```text
PopulationBoom
PopulationCrash
SpeciesExtinct
NewSpecies
MassMigration
FoodShortage
Drought
```

UI posiada timeline:

```text
Year 184
Herbivore population exceeded 10,000

Year 221
Carnivore population increased 340%

Year 247
Herbivore population collapsed

Year 261
Species #7 became extinct
```

---

# 25. God Mode

Użytkownik może ingerować w świat.

Przykładowe akcje:

```text
Spawn creatures
Remove creatures

Add vegetation
Remove vegetation

Create water source
Remove water source

Change temperature

Start drought
```

Dzięki temu symulacja staje się laboratorium.

Przykład eksperymentu:

> Co stanie się z ekosystemem, jeżeli usuniemy wszystkie drapieżniki?

---

# 26. World Configuration

Przed rozpoczęciem:

```text
World Seed
Map Size

Initial Herbivores
Initial Carnivores

Vegetation Density
Water Density

Mutation Rate
Mutation Strength
```

Opcjonalnie:

```text
Random Seed
```

pozwala wygenerować nowy eksperyment.

---

# 27. Replay / Time Machine

**Nie jest wymagane dla MVP.**

Docelowo system zapisuje snapshoty świata.

Timeline:

```text
Year

0 ━━━━━━━━━━━━━●━━━━━━━━━━━━━━━━━━ 5000
               1284
```

Użytkownik może cofnąć się i zobaczyć wcześniejszy stan ekosystemu.

Może dzięki temu obserwować ewolucję populacji.

---

# 28. Evolution Tree

**Future milestone / portfolio feature.**

System wizualizuje historię gatunków:

```text
                 ┌── Species 14
Species 1 ───────┤
                 │       ┌── Species 31
                 └───────┤
                         └── Species 47
```

Kliknięcie gatunku:

* pokazuje jego historię,
* podświetla organizmy,
* pokazuje średni genom,
* pokazuje przodków,
* pokazuje potomne gatunki.

---

# 29. Architecture

Proponowany podział backendu:

```text
WildSeed.Domain

World
Creature
Genome
Species
Environment

WildSeed.Simulation

SimulationEngine
MovementSystem
NeedsSystem
PerceptionSystem
BehaviourSystem
FeedingSystem
HuntingSystem
CombatSystem
ReproductionSystem
MutationSystem
DeathSystem
VegetationSystem

WildSeed.Analytics

PopulationTracker
GenomeTracker
EventDetector

WildSeed.Api

REST
SignalR
SimulationController
```

Frontend:

```text
src/

simulation/
map/
creatures/
species/
charts/
timeline/
controls/
inspector/
```

---

# 30. Simulation Loop

Podstawowy flow:

```text
Tick
 │
 ├── Environment
 │
 ├── Needs
 │
 ├── Perception
 │
 ├── Decision
 │
 ├── Movement
 │
 ├── Interaction
 │
 ├── Hunting / Combat
 │
 ├── Feeding
 │
 ├── Reproduction
 │
 ├── Death
 │
 └── Statistics
 │
 ▼
Next Tick
```

Kolejność systemów powinna być jawna i deterministyczna.

---

# 31. MVP — v0.1

Pierwsza wersja powinna udowodnić tylko jedną rzecz:

> **Czy z prostych zasad powstaje interesujący, dynamiczny ekosystem?**

Zakres:

* proceduralna mapa,
* land,
* water,
* vegetation,
* herbivores,
* carnivores,
* movement,
* vision,
* hunger,
* thirst,
* energy,
* eating,
* drinking,
* hunting,
* fleeing,
* reproduction,
* death,
* prosty genome,
* inheritance,
* mutations,
* population statistics,
* mapa renderowana w PixiJS,
* pause,
* x1,
* x10,
* MAX,
* creature inspector.

### Poza MVP

Nie implementować początkowo:

* pogody,
* pór roku,
* chorób,
* stad,
* terytoriów,
* genetyki kolorów,
* specjacji,
* drzewa ewolucyjnego,
* replay,
* zapisywania światów,
* multiplayer,
* skomplikowanego combat system,
* LLM/AI.

---

# 32. Success Criteria v0.1

MVP uznajemy za udane, jeśli:

1. Symulacja może działać przez wiele pokoleń bez ingerencji użytkownika.
2. Populacje dynamicznie reagują na dostępność zasobów i siebie nawzajem.
3. Organizmy przekazują genom potomstwu.
4. Mutacje powodują mierzalne zmiany średnich cech populacji.
5. Presja środowiska może doprowadzić do dominacji określonych cech.
6. Możliwe jest wymarcie populacji.
7. Ten sam seed daje deterministyczny wynik.
8. Użytkownik może obserwować symulację w realtime.
9. Możliwe jest uruchomienie symulacji znacznie szybciej niż realtime.
10. Kliknięcie organizmu pozwala zobaczyć jego aktualny stan i genom.

---

# 33. North Star Demo

Docelowe demo portfolio powinno pozwalać użytkownikowi:

1. wygenerować świat,
2. uruchomić symulację,
3. przyspieszyć ją,
4. obserwować polowania, migrację i zmiany populacji,
5. zatrzymać świat po setkach pokoleń,
6. kliknąć istniejący organizm,
7. zobaczyć jego genom,
8. zobaczyć, jak jego gatunek zmienił się od początku symulacji,
9. wywołać zmianę środowiska,
10. obserwować reakcję ekosystemu.

Główne wrażenie:

> **„Stworzyłem zasady świata. Nie stworzyłem historii, która się w nim wydarzyła.”**
