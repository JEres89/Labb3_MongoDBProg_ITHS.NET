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

## Method

### 1. Prepare the game state for serialization
All classes which should be saved to the database needs to be prepared for serialization. 
Applying attributes to the classes and properties to control which and how they are serialized and deserialized.

While trying to keep the changes to existing code reasonably few this is what I have arrived at:
- Add __Id__ field to ___LevelEntity___
- Add Bson attributes to ___LevelEntity___ and all inheriting classes
	a. _BsonDiscriminator_ and _BsonKnownTypes_ to ___LevelEntity___ to handle polymorphism
	b. _BsonId_ to __Id__ field (note: must be matched as "_id" in filters etc)
	c. _BsonConstructor_ to most classes since they dont have public setters for properties
	d. __Pos__ and __Symbol__ from ___LevelElement___ works as is
- Move the __Turn__ property from ___Player___ to ___Level___ (don't know why I put it there in the first place) so that it will be serialized at the root of the Level document
- Change ___CombatResult___ to store all values needed to generate the message in int arrays
- Add __List\<Position> Walls__ to Level to avoid saving all walls as separate entities since they have no identifying values, and populate it at initialization to make use of the span from ___LevelReader___
- Add public properties to Level for ___enemies__ and ___discovered__ for MongoDB serialization. Because of the lack of information on how MongoDB handles 2D arrays, I won't save any data in that way. ___discovered__ I will convert to a linear array and convert it back to 2D when loading the game, ___elements__ will be reconstructed from ___enemies__ and the new __Walls__ list.
- Add calling the MongoClient and a menu to the ___Program___ class to handle loading games

The biggest change will probably be to the message log:
- To handle scrolling back and forth in the log and saving it, I will implement a ___LogMessageBase___ record which ___CombatResult___ and the new ___LogMessage___ will inherit from. It will store all information needed to regenerate any message, but not the actual string to preserve data.
- These will be stored in a ___MessageLog___ class while the ___Renderer___ will request the messages from the log to render them while keeping a number of the already generated messages in cache for scrolling.
- I will have to rewrite the rendering of the log to use the new class.

### 2. Implement MongoDB connection
___GameMongoClient___ will handle the connection to the MongoDB server and the database. It will have methods to save and load the game state.
It will also have a method ___EnsureCreated___ verifying that the server can be connected to and that the database exists, creating it if it doesn't.

### 3. Establish format of saved game state in MongoDB
I will use one collection for a metadata documents in the form of SaveObject, and one separate collection for each save game. 
The SaveObject will contain some identifying information as well as the name of the collection for the data.
The collections for savegames will either have a few array-documents with different types of entities, or everything nested in the Level document, as well as one document for the log messages.
Because there won't be any need to access only parts of the data at any one time, I will try the latter first. Also because if all goes well MongoDB Driver should be able to serialize it all directly from the Level object.

### 4. Implement saving and loading game state
- Modified the ___InputHandler___ and ___IInputEndpoint___ to also handle listeners for key commands (key + alt/shift/ctrl) which take precedence over normal key listeners. I.e. the ___Player___ won't receive the key 'S' if the listener for 'Ctrl+S' (save) is active.


