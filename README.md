# .NET Project

# Restaurant Management Systeem - Chez Antoine

Een volledig restaurant management systeem ontwikkeld met ASP.NET Core MVC voor het beheren van reservaties, bestellingen, menu's en personeel.

## Projectbeschrijving

Dit project is een compleet restaurant management systeem dat verschillende aspecten van een restaurant digitaliseert:
- **Klanten** kunnen online reservaties maken en hun reservaties beheren
- **Personeel** kan bestellingen beheren via verschillende rollen (Ober, Kok, Zaalverantwoordelijke)
- **Eigenaren** hebben volledige controle over het systeem, inclusief gebruikersbeheer en menu-aanpassingen
- **Geautomatiseerde e-mailcommunicatie** voor bevestigingen, welkomstberichten en evaluaties

## Technologieen

- **Framework:** ASP.NET Core 8.0 MVC
- **Database:** SQL Server met Entity Framework Core
- **Authentication:** ASP.NET Core Identity (Role-based)
- **Real-time communicatie:** SignalR (voor keuken updates)
- **E-mail:** SMTP met template-gebaseerde mails
- **Front-end:** Bootstrap 5, Custom CSS
- **Mapping:** AutoMapper
- **Architectuur:** Repository Pattern met Unit of Work

## Belangrijkste Features

### Voor Klanten
- Online reservaties maken met datum en tijdslot selectie
- Reservaties bekijken en beheren
- QR-code scanning voor digitaal menu
- Bestellingen plaatsen tijdens het diner
- Evaluaties achterlaten na bezoek

### Voor Personeel
- **Ober:** Bestellingen opnemen en status bijwerken
- **Kok:** Keukenbestellingen bekijken en verwerken (real-time via SignalR)
- **Zaalverantwoordelijke:** Tafels toewijzen aan reservaties, klanten inchecken
- **Eigenaar:** Volledige systeem controle, gebruikersbeheer, menu beheer

### Administratie
- Menu beheer (gerechten, dranken, desserten, specials)
- Categorieën en prijsbeheer
- Tafelbeheer (capaciteit, status, QR-codes)
- Tijdslot configuratie
- Sluitingsdagen instellen
- Gebruikersbeheer met rol-toewijzing
- E-mail template beheer

### Automatisering
- **Background Scheduler:** Dagelijks versturen van welkomstmails (06:00 uur)
- **E-mail Workflow:**
  - Bevestigingsmail na reservatie
  - Welkomstmail op dag van reservatie
  - Bedanktmail met factuur na betaling
  - Evaluatiemail met survey link

## Projectstructuur

```
Restaurant/
├── Controllers/        # MVC Controllers (business logica)
├── ViewModels/         # Data transfer objects voor views
├── Models/             # Database entiteiten
├── Data/               # DbContext, Migrations, Seeding
├── Repositories/       # Data access layer (Repository Pattern)
├── Services/           # Business services (Mail, Templates, etc.)
├── Hubs/               # SignalR hubs voor real-time communicatie
├── Views/              # Razor views
├── wwwroot/            # Static files (CSS, JS, Images)
└── Migrations/         # Entity Framework migrations
```

## Design Patterns

- **Repository Pattern:** Abstractie van data access logic
- **Unit of Work:** Gecoördineerde transacties over meerdere repositories
- **Dependency Injection:** Loose coupling en testbaarheid
- **AutoMapper:** Object-to-object mapping
- **Background Services:** Scheduled tasks voor e-mail verzending

# Opstart applicatie

## Vereisten
- Visual Studio 2022 of hoger
- .NET 8 SDK
- SQL Server (LocalDB of SQL Server Express)
- SMTP server voor e-mail functionaliteit (optioneel voor testing)

## Installatie Stappen
1. Clone de repository
2. Open de solution in Visual Studio
3. Update de connection string in `appsettings.json` indien nodig
4. Open Package Manager Console en voer uit:
   ```
   Update-Database
   ```
5. Run `InitialData.sql` in SQL Server Management Studio of via Visual Studio
6. Start de applicatie (F5) - dit triggert de Identity seeding
7. Stop de applicatie en run `TestData.sql` (optioneel, voor test data)
8. Start de applicatie opnieuw

## Configuratie

### Database Connection String
Pas `appsettings.json` aan indien nodig:
```json
"ConnectionStrings": {
  "LocalDBConnection": "Server=(localdb)\\mssqllocaldb;Database=RestaurantDB;Trusted_Connection=True;"
}
```

### SMTP Configuratie (optioneel)
Voor e-mail functionaliteit, configureer SMTP instellingen in `appsettings.json`.

# Login Gegevens

## Test Accounts
De volgende test accounts zijn beschikbaar na het uitvoeren van database seeding:

### Eigenaar (Alle rollen)
- **Email:** restaurant.testmail.tm+eigenaar@gmail.com
- **Wachtwoord:** Ww#123
- **Rollen:** Eigenaar, Ober, Kok, Zaalverantwoordelijke, Klant

### Zaalverantwoordelijke
- **Email:** restaurant.testmail.tm+zaalverantwoordelijke@gmail.com
- **Wachtwoord:** Ww#123
- **Rollen:** Zaalverantwoordelijke, Klant

### Ober
- **Email:** restaurant.testmail.tm+ober@gmail.com
- **Wachtwoord:** Ww#123
- **Rollen:** Ober, Klant

### Kok
- **Email:** restaurant.testmail.tm+kok@gmail.com
- **Wachtwoord:** Ww#123
- **Rollen:** Kok, Klant

### Klant
- **Email:** restaurant.testmail.tm+klant@gmail.com
- **Wachtwoord:** Ww#123
- **Rollen:** Klant

### Tester
- **Email:** Tester@restaurant.be
- **Wachtwoord:** Tester@123
- **Rollen:** Ober, Kok, Zaalverantwoordelijke, Klant

### Michiel Wouters (Thomas More - Alle rollen)
- **Email:** michiel.wouters@thomasmore.be
- **Wachtwoord:** Michiel@123
- **Rollen:** Eigenaar, Ober, Kok, Zaalverantwoordelijke, Klant

## E-mail Workflow voor Michiel Wouters Account

Om e-mails te ontvangen op het account van Michiel Wouters, dient de volgende workflow doorlopen te worden:

### 1. Reservatie Maken
- Log in met het **Michiel Wouters account** (michiel.wouters@thomasmore.be)
- Dit account heeft alle rollen, inclusief de Klant rol
- Maak een reservatie aan via de reservatiepagina
- **Belangrijk**: Selecteer **vandaag** als datum
- Selecteer een tijdslot en aantal personen
- Reservatie wordt aangemaakt in het systeem
- 1ste mail (bevestiging) wordt automatisch verstuurd

### 2. Tafel Toewijzen (Zaalverantwoordelijke)
- Blijf ingelogd op hetzelfde **Michiel Wouters account**
- Dit account heeft ook de Zaalverantwoordelijke rol
- Ga naar het overzicht van reservaties
- Wijs een of meerdere tafels toe aan de reservatie

### 3. Betaling Afronden
- Na afloop van de reservatie, rond de betaling af
- Dit triggert automatisch de **bedanktmail** naar de email van Michiel Wouters (persoon hangt af van de reservatiegegevens)
- In deze mail wordt de klant bedankt voor het bezoek
- De mail bevat ook een link/uitnodiging om een **evaluatie (survey)** in te vullen

### 4. Evaluatie Invullen (Optioneel)
- Via de bedanktmail kan de klant een evaluatie achterlaten
- Open de evaluatielink vanuit de e-mail
- Vul de survey in met:
  - Aantal sterren (rating)
  - Optionele opmerkingen
- Dit helpt het restaurant om de service te verbeteren

### Belangrijke Opmerkingen:
- **SMTP configuratie**: Zorg dat de SMTP instellingen correct zijn geconfigureerd in `appsettings.json`
- **E-mail templates**: E-mail templates kunnen beheerd worden via de Mail-tabel in de database
- **Testing**: Voor testing van e-mails kan je de instellingen aanpassen om naar een test e-mailadres te sturen
- **Scheduler**: De welkomstmail scheduler draait automatisch bij opstarten van de applicatie om 06:00 uur
- **Alle rollen in één account**: Omdat het Michiel Wouters account alle rollen heeft, hoef je niet tussen accounts te wisselen voor het testen van de volledige workflow
- **Reservatiedatum**: De reservatie moet op **vandaag** staan om in aanmerking te komen voor een welkomstmail
- **E-mail flow**: Bevestigingsmail -> Welkomstmail -> Bedanktmail (na betaling) -> Evaluatie (optioneel)

# Stijlregels
- Nooit rechtstreeks op dev of op main pushen
- Van eigen branch pull request naar Dev
- Branch benaming : XX-IssueNaam | waar XX de initialen zijn van de developer
- Op bepaalde tijdstippen Dev mergen naar Main. (!!! Nooit van main naar Dev)
- Een pullrequest mag max 3 werkdagen openstaan
- Eerst functionele eisen in MVC , daarna UI verfijnen in frontend

# Code Guideline
- Db Context moet gebruikt worden
- Repo's gebruiken voor com met db
- Repo's worden Repository genoemd en niet Repo
- ViewModel files worden voluit geschreven.
- models
- Controllers -> businessLogica
- Automapper en VMs ook gebruiken

# Definition of Done
- Pull Requests moeten door minstens 1 persoon nagekeken worden - Reviewer draagt evenveel verantwoordelijkheid als schrijver
- Reviewer sluit de pull request , nooit de schrijver
- Code compileert zonder fouten (er mogen geen Errors in Dev zitten)

## Licentie
Dit project is ontwikkeld voor educatieve doeleinden - Thomas More Graduaten IT

## Team
NOT Developers - AJ2526 C# .NET Project
