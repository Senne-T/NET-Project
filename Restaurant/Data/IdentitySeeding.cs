using System.Data.Common;

namespace Restaurant.Data
{
    public class IdentitySeeding
    {
        public async Task IdentitySeedingAsync(UserManager<CustomUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            try
            {
                // Eigenaar aanmaken
                // Kijken of er al een eigenaar is
                if (userManager.FindByNameAsync("restaurant.testmail.tm+eigenaar@gmail.com").Result == null)
                {
                    // Gebruiker maken
                    var defaultUser = new CustomUser
                    {
                        UserName = "restaurant.testmail.tm+eigenaar@gmail.com",
                        Achternaam = "De eigenaar",
                        Voornaam = "Eigenaar",
                        Email = "restaurant.testmail.tm+eigenaar@gmail.com",
                        LandId = 1,
                        Actief = true,
                        EmailConfirmed = true,
                        PhoneNumberConfirmed = true,
                    };

                    // Eigenaar toevoegen aan db
                    var createResult = await userManager.CreateAsync(defaultUser, "Ww#123");

                    if (createResult.Succeeded)
                    {
                        // Rollen geven
                        string[] roles = ["Eigenaar", "Ober", "Kok", "Zaalverantwoordelijke", "Klant"];

                        foreach (var role in roles)
                        {
                            // Kijken of rol al bestaat
                            if (!await roleManager.RoleExistsAsync(role))
                            {
                                // Rol aanmaken
                                await roleManager.CreateAsync(new IdentityRole(role));
                            }
                        }

                        foreach (var role in roles)
                        {
                            await userManager.AddToRoleAsync(defaultUser, role);
                        }
                    }
                }
            }
            catch (DbException ex)
            {
                throw ex;
            }

            try
            {
                // Zaalverantwoordelijke aanmaken
                // Kijken of er al een zaalverantwoordelijke is
                if (userManager.FindByNameAsync("restaurant.testmail.tm+zaalverantwoordelijke@gmail.com").Result == null)
                {
                    // Gebruiker maken
                    var defaultUser = new CustomUser
                    {
                        UserName = "restaurant.testmail.tm+zaalverantwoordelijke@gmail.com",
                        Achternaam = "De zaalverantwoordelijke",
                        Voornaam = "Zaalverantwoordelijke",
                        Email = "restaurant.testmail.tm+zaalverantwoordelijke@gmail.com",
                        LandId = 1,
                        Actief = true,
                        EmailConfirmed = true,
                        PhoneNumberConfirmed = true,
                    };

                    // Zaalverantwoordelijke toevoegen aan db
                    var createResult = await userManager.CreateAsync(defaultUser, "Ww#123");

                    if (createResult.Succeeded)
                    {
                        // Rollen geven
                        string[] roles = ["Zaalverantwoordelijke", "Klant"];

                        foreach (var role in roles)
                        {
                            // Kijken of rol al bestaat
                            if (!await roleManager.RoleExistsAsync(role))
                            {
                                // Rol aanmaken
                                await roleManager.CreateAsync(new IdentityRole(role));
                            }
                        }

                        foreach (var role in roles)
                        {
                            await userManager.AddToRoleAsync(defaultUser, role);
                        }
                    }
                }
            }
            catch (DbException ex)
            {
                throw ex;
            }

            try
            {
                // Ober aanmaken
                // Kijken of er al een ober is
                if (userManager.FindByNameAsync("restaurant.testmail.tm+ober@gmail.com").Result == null)
                {
                    // Gebruiker maken
                    var defaultUser = new CustomUser
                    {
                        UserName = "restaurant.testmail.tm+ober@gmail.com",
                        Achternaam = "De ober",
                        Voornaam = "Ober",
                        Email = "restaurant.testmail.tm+ober@gmail.com",
                        LandId = 1,
                        Actief = true,
                        EmailConfirmed = true,
                        PhoneNumberConfirmed = true,
                    };

                    // Ober toevoegen aan db
                    var createResult = await userManager.CreateAsync(defaultUser, "Ww#123");

                    if (createResult.Succeeded)
                    {
                        // Rollen geven
                        string[] roles = ["Ober", "Klant"];

                        foreach (var role in roles)
                        {
                            // Kijken of rol al bestaat
                            if (!await roleManager.RoleExistsAsync(role))
                            {
                                // Rol aanmaken
                                await roleManager.CreateAsync(new IdentityRole(role));
                            }
                        }

                        foreach (var role in roles)
                        {
                            await userManager.AddToRoleAsync(defaultUser, role);
                        }
                    }
                }
            }
            catch (DbException ex)
            {
                throw ex;
            }

            try
            {
                // Kok aanmaken
                // Kijken of er al een kok is
                if (userManager.FindByNameAsync("restaurant.testmail.tm+kok@gmail.com").Result == null)
                {
                    // Gebruiker maken
                    var defaultUser = new CustomUser
                    {
                        UserName = "restaurant.testmail.tm+kok@gmail.com",
                        Achternaam = "De kok",
                        Voornaam = "Kok",
                        Email = "restaurant.testmail.tm+kok@gmail.com",
                        LandId = 1,
                        Actief = true,
                        EmailConfirmed = true,
                        PhoneNumberConfirmed = true,
                    };

                    // Kok toevoegen aan db
                    var createResult = await userManager.CreateAsync(defaultUser, "Ww#123");

                    if (createResult.Succeeded)
                    {
                        // Rollen geven
                        string[] roles = ["Kok", "Klant"];

                        foreach (var role in roles)
                        {
                            // Kijken of rol al bestaat
                            if (!await roleManager.RoleExistsAsync(role))
                            {
                                // Rol aanmaken
                                await roleManager.CreateAsync(new IdentityRole(role));
                            }
                        }

                        foreach (var role in roles)
                        {
                            await userManager.AddToRoleAsync(defaultUser, role);
                        }
                    }
                }
            }
            catch (DbException ex)
            {
                throw ex;
            }

            try
            {
                // Klant aanmaken
                // Kijken of er al een klant is
                if (userManager.FindByNameAsync("restaurant.testmail.tm+klant@gmail.com").Result == null)
                {
                    // Gebruiker maken
                    var defaultUser = new CustomUser
                    {
                        UserName = "restaurant.testmail.tm+klant@gmail.com",
                        Achternaam = "De klant",
                        Voornaam = "Klant",
                        Email = "restaurant.testmail.tm+klant@gmail.com",
                        LandId = 1,
                        Actief = true,
                        EmailConfirmed = true,
                        PhoneNumberConfirmed = true,
                    };

                    // Klant toevoegen aan db
                    var createResult = await userManager.CreateAsync(defaultUser, "Ww#123");

                    if (createResult.Succeeded)
                    {
                        // Rollen geven
                        string[] roles = ["Klant"];

                        foreach (var role in roles)
                        {
                            // Kijken of rol al bestaat
                            if (!await roleManager.RoleExistsAsync(role))
                            {
                                // Rol aanmaken
                                await roleManager.CreateAsync(new IdentityRole(role));
                            }
                        }

                        foreach (var role in roles)
                        {
                            await userManager.AddToRoleAsync(defaultUser, role);
                        }
                    }
                }
            }
            catch (DbException ex)
            {
                throw ex;
            }

            try
            {
                // Tester aanmaken
                // Kijken of er al een tester is
                if (userManager.FindByNameAsync("Tester@restaurant.be").Result == null)
                {
                    // Gebruiker maken
                    var defaultUser = new CustomUser
                    {
                        UserName = "Tester@restaurant.be",
                        Achternaam = "De Tester",
                        Voornaam = "Tester",
                        Email = "Tester@restaurant.be",
                        LandId = 1,
                        Actief = true,
                        EmailConfirmed = true,
                        PhoneNumberConfirmed = true,
                    };

                    // Tester toevoegen aan db
                    await userManager.CreateAsync(defaultUser, "Tester@123");

                    // Rollen geven
                    string[] roles = ["Ober", "Kok", "Zaalverantwoordelijke", "Klant"];

                    foreach (var role in roles)
                    {
                        // Kijken of rol al bestaat
                        if (!await roleManager.RoleExistsAsync(role))
                        {
                            // Rol aanmaken
                            await roleManager.CreateAsync(new IdentityRole(role));
                        }
                    }

                    foreach (var role in roles)
                    {
                        await userManager.AddToRoleAsync(defaultUser, role);
                    }
                }
            }
            catch (DbException ex)
            {
                throw ex;
            }

            try
            {
                // Michiel Wouters aanmaken
                // Kijken of er al een Michiel Wouters is
                if (userManager.FindByNameAsync("michiel.wouters@thomasmore.be").Result == null)
                {
                    // Gebruiker maken
                    var defaultUser = new CustomUser
                    {
                        UserName = "michiel.wouters@thomasmore.be",
                        Achternaam = "Wouters",
                        Voornaam = "Michiel",
                        Email = "michiel.wouters@thomasmore.be",
                        LandId = 1,
                        Actief = true,
                        EmailConfirmed = true,
                        PhoneNumberConfirmed = true,
                    };

                    // Michiel Wouters toevoegen aan db
                    var createResult = await userManager.CreateAsync(defaultUser, "Michiel@123");

                    if (createResult.Succeeded)
                    {
                        // Rollen geven
                        string[] roles = ["Eigenaar", "Ober", "Kok", "Zaalverantwoordelijke", "Klant"];

                        foreach (var role in roles)
                        {
                            // Kijken of rol al bestaat
                            if (!await roleManager.RoleExistsAsync(role))
                            {
                                // Rol aanmaken
                                await roleManager.CreateAsync(new IdentityRole(role));
                            }
                        }

                        foreach (var role in roles)
                        {
                            await userManager.AddToRoleAsync(defaultUser, role);
                        }
                    }
                }
            }
            catch (DbException ex)
            {
                throw ex;
            }
        }
    }
}