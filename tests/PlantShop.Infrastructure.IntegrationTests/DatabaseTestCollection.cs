namespace PlantShop.Infrastructure.IntegrationTests;

//+ara teste correr à vez em vez de paralelo de forma a evita deadlock da BD
[CollectionDefinition("DatabaseTests")]
public class DatabaseTestCollection
{
}
