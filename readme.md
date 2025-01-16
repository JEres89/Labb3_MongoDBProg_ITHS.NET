# Assignment 3 - MongoDB
## Assignment description
### Labb 3 - Utveckla en databasapp som använder MongoDB

I denna labb utvecklar vi en applikation i C# som använder MongoDB.Driver och/eller Entity Framework för att låta användaren läsa och uppdatera data i en MongoDB-databas.

Ni väljer själva om ni vill göra en Console- eller WPF-App, så länge funktionaliteten är på plats. Ni kan välja på ett av de föreslagna projekten nedan, eller hitta på en egen idé.

OBS! Applikationen ska inte kräva någon befintlig databas. Den ska ansluta mot MongoDB på localhost och själv skapa en databas och populera denna med eventuell demodata (om den inte redan existerar). Viktigt: Namnet på databasen som skapas ska vara ditt  för- och efternamn (Exempel: "FredrikJohansson"). Därefter ska appen använda den existerande databasen i kommande körningar.


#### Dungeon Crawler med utökad funktionalitet.

Uppdatera Dungeon Crawler från C# Labb 2 så att spelaren kan spara och avsluta spelet, och senare starta och fortsätta där man befann sig. Samtliga "LevelElements" ska lagras i det tillstånd de befinner sig: för väggar lagras information om de är "upptäckta" eller inte; för fiender lagras position, hälsa (HP) och eventuellt annat som behövs för att fortsätta där man var. För spelaren lagras namn, position, hälsa, eventuella uppgraderingar och antal utförda drag (turns). När man startar spelet nästa gång ska allt laddas in och förberedas så att man kan fortsätta spelet där man avslutade.

För VG ska ytterligare två funktioner implementeras i spelet (och lagras i MongoDB):

1) Spelet ska ha stöd för flera samtidigt sparade spel. När man startar spelet får man välja om man vill skapa en ny karaktär (med nytt namn) eller fortsätta spela med en av de tidigare karaktärerna som ännu inte dött. Om man avslutar ett spel sparas detta i databasen, men om man dör med en karaktär så går denna inte längre att ladda in.

2) Spelet ska ha en meddelande logg där man kan scrolla tillbaka och se alla tidigare meddelanden (till exempel när man attackerar och försvarar sig). Loggen kan visas hela tiden vid sidan av spelplanen, eller öppnas (i helskärm) via en knapp avsedd för detta. Loggen sparas för varje karaktär och laddas in i sin helhet vid nästa tillfälle.


## Initial analysis

### Features to implement:

- Save and load game state
    1. All game elements should be saved in their current state, including:
		a. Walls (position, discovered or not)
		b. Enemies (position, health, etc.)
		c. Player (name, position, health, upgrades, turns)
		d. Log messages 
		e. (Later) All constant values for initial states (entity health etc.)
		f. Map data from Level class
	2. This requires a way to serialize and deserialize the game state
		a. State values needs to be available in a format that can be saved to a database, so for example if a connection is made in game by an object reference, this needs to be converted to a unique identifier
- Multiple save slots
- MongoDB connection to localhost using MongoDB.Driver 
	1. Create or use existing database with name "FirstnameLastname"
- Make the message log scrollable
- Extended menu and key inputs for saving, loading and viewing the message log

### Method

#### 1. Prepare the game state for serialization
All classes which should be saved to the database needs to be prepared for serialization. 
Applying attributes to the classes and properties to control which and how they are serialized and deserialized.

While trying to keep the changes to existing code reasonably few this is what I have arrived at:
- Add Id field to LevelEntity
- Add Bson attributes to LevelEntity and all inheriting classes
	a. BsonDiscriminator and BsonKnownTypes to LevelEntity to handle polymorphism
	b. BsonId to Id field (note: must be matched as "_id" in filters etc)
	c. BsonConstructor to most classes since they dont have public setters for properties
	d. Pos and Symbol from LevelElement works as is
- Move the Turn property from Player to Level (don't know why I put it there in the first place) so that it will be serialized at the root of the Level document
- Change CombatResult to store all values needed to generate the message in int arrays
- Add List<Position> Walls to Level to avoid saving all walls as separate entities since they have no identifying values and populating it at initialization to make use of the span from LevelReader
- Add public properties to Level for _enemies and _discovered for MongoDB serialization. Because of the lack of information on how MongoDB handles 2D arrays, I will save
- Add GameMongoClient and a menu to the Program class to handle loading games 

#### 2. Implement MongoDB connection


#### 3. Establish format of saved game state in MongoDB
I will use one collection for a metadata documents in the form of SaveObject, and one separate collection for each save game. 
The SaveObject will contain some identifying information as well as the name of the collection for the data.
The collections for savegames will either have a few array-documents with different types of entities, or everything nested in the Level document. 
Because there won't be any need to access only parts of the data at any one time, I will try the latter first.


#### 4. Implement saving and loading game state








# LATER

## Implementation

### Wall / Static objects

Since walls does not have any variable internal state, they will simply be saved as a list of positions in the level document. When loading the game, the walls will be created at these positions with the standard constructor.

### LevelEntity

Entity will have an added Id field used for saving and loading as well as an abstract method to convert the entity to a Bson document. The entity will also have an abstract method to instantiate from a document.

Values which does not change for the same entity type are set when the entity is created from the document.

#### Bson document structure

	{ "Id", Id },
	{ "Type", nameof('class') },
	{ "Pos", {X, Y},
	{ "Stats", int[] {
		Health,
		AttackDieSize,
		AttackDieNum,
		AttackMod,
		DefenseDieSize,
		DefenseDieNum,
		DefenseMod }
	},
	{ // unique type values are added last }

### Combat Messages (record CombatResult)

Since the combat messages are now generated only once, when they happen, and rely on having the attacker and defender entities available, how it stores data will have to be rewritten.

To make the parsing from and to Bson easy, and to save space, the record will store all the information needed to generate the message in mostly int arrays, and generate the message with the same method. 

The only change visible externally is that the turn might have to be added to the constructor.

#### Bson document structure

	{ "attName", attackerName },
	{ "defName", defenderName },
	{ "effectIndex", effectIndex },
	{ "attStats", int[] {
		attacker.Id,
		attacker.AttackDieSize, 
		attacker.AttackDieNum, 
		attacker.AttackMod,
		attackRoll,
		damage }
	},
	{ "defStats", int[] {
		defender.Id,
		defender.DefenseDieSize, 
		defender.DefenseDieNum, 
		defender.DefenseMod,
		defenseRoll }
	}

## Serializers

### LevelEntitySerializer
