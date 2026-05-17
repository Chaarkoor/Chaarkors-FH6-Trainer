namespace FH6Mod.Cheats.Sql;

public enum SqlFeature
{
    ClearNewTag,
    FreeCarPrices,
    InstallFlags,
    AddAllCars,
    AutoshowUnlock,
}

/// <summary>
/// Pre-baked SQL bundles. Each maps a high-level feature to one or more
/// queries. Order matters — backup table first, then mutation.
/// </summary>
internal static class SqlFeatureCatalog
{
    public sealed record Feature(string Name, string Description, string[] Queries);

    public static Feature Get(SqlFeature f) => f switch
    {
        SqlFeature.ClearNewTag => new(
            "Clear \"New\" Tag on Cars",
            "Marks every garage car as 'already viewed' so the persistent NEW! badge disappears.",
            [
                "CREATE TABLE IF NOT EXISTS _backup_NewTags AS SELECT CarId, HasCurrentOwnerViewedCar FROM Profile0_Career_Garage;",
                "UPDATE Profile0_Career_Garage SET HasCurrentOwnerViewedCar = 1 WHERE HasCurrentOwnerViewedCar IS NULL OR HasCurrentOwnerViewedCar <> 1;",
            ]),

        SqlFeature.FreeCarPrices => new(
            "Free Car Prices",
            "Sets BaseCost = 0 on every car in Data_Car. New autoshow purchases are free.",
            [
                "CREATE TABLE IF NOT EXISTS _backup_Database_FreeCarPrices AS SELECT Id, BaseCost FROM Data_Car;",
                "UPDATE Data_Car SET BaseCost = 0;",
            ]),

        SqlFeature.InstallFlags => new(
            "Install Flags (all cars installed/purchased/drivable)",
            "Flips IsInstalled/IsPurchased/IsDrivable to 1 for every car in Data_Car. Removes 'install required' gates.",
            [
                "CREATE TABLE IF NOT EXISTS _backup_DataCarIsInstalled AS SELECT Id, IsInstalled FROM Data_Car;",
                "UPDATE Data_Car SET IsInstalled = 1 WHERE IsInstalled IS NULL OR IsInstalled <> 1;",
                "CREATE TABLE IF NOT EXISTS _backup_DataCarIsPurchased AS SELECT Id, IsPurchased FROM Data_Car;",
                "UPDATE Data_Car SET IsPurchased = 1 WHERE IsPurchased IS NULL OR IsPurchased <> 1;",
                "CREATE TABLE IF NOT EXISTS _backup_DataCarIsDrivable AS SELECT Id, IsDrivable FROM Data_Car;",
                "UPDATE Data_Car SET IsDrivable = 1 WHERE IsDrivable IS NULL OR IsDrivable <> 1;",
            ]),

        SqlFeature.AddAllCars => new(
            "Add All Cars (grant every car free)",
            "Marks every car for free auto-redeem grant. Reopen the game and visit Autoshow/Garage to claim.",
            [
                "CREATE TABLE IF NOT EXISTS _backup_AddAllCars_FreeCars AS SELECT * FROM Profile0_FreeCars;",
                "INSERT OR IGNORE INTO Profile0_FreeCars (CarId, FreeCount) SELECT Id, 1 FROM Data_Car WHERE Id <> 3300 AND Id NOT IN (SELECT CarId FROM Profile0_FreeCars WHERE CarId IS NOT NULL);",
                "UPDATE Profile0_FreeCars SET FreeCount = 1 WHERE FreeCount IS NULL OR FreeCount < 1;",
            ]),

        SqlFeature.AutoshowUnlock => new(
            "Autoshow — All Cars Visible",
            "Removes the 'NotAvailableInAutoshow' and 'VisibleOnlyIfOwned' filters; every car shows up in showroom listings.",
            [
                "CREATE TABLE IF NOT EXISTS _backup_AutoshowState AS SELECT Id, NotAvailableInAutoshow, BaseCost FROM Data_Car;",
                "UPDATE Data_Car SET NotAvailableInAutoshow = 0;",
                "CREATE TABLE IF NOT EXISTS _backup_DataCarVisibleOnlyIfOwned AS SELECT Id, VisibleOnlyIfOwned FROM Data_Car;",
                "UPDATE Data_Car SET VisibleOnlyIfOwned = 0;",
            ]),

        _ => throw new System.InvalidOperationException("Unknown SQL feature."),
    };

    /// <summary>
    /// Revert queries — restore from _backup_* tables that <see cref="Get"/> creates.
    /// Used when a toggle-mode lock is turned OFF so the game returns to pre-cheat state.
    /// Returns empty array for features that don't need revert (one-shots).
    /// </summary>
    public static string[] GetRevert(SqlFeature f) => f switch
    {
        SqlFeature.FreeCarPrices =>
        [
            "UPDATE Data_Car SET BaseCost = (SELECT BaseCost FROM _backup_Database_FreeCarPrices WHERE _backup_Database_FreeCarPrices.Id = Data_Car.Id) WHERE EXISTS (SELECT 1 FROM _backup_Database_FreeCarPrices WHERE _backup_Database_FreeCarPrices.Id = Data_Car.Id);",
        ],
        SqlFeature.AutoshowUnlock =>
        [
            "UPDATE Data_Car SET NotAvailableInAutoshow = (SELECT NotAvailableInAutoshow FROM _backup_AutoshowState WHERE _backup_AutoshowState.Id = Data_Car.Id) WHERE EXISTS (SELECT 1 FROM _backup_AutoshowState WHERE _backup_AutoshowState.Id = Data_Car.Id);",
            "UPDATE Data_Car SET VisibleOnlyIfOwned = (SELECT VisibleOnlyIfOwned FROM _backup_DataCarVisibleOnlyIfOwned WHERE _backup_DataCarVisibleOnlyIfOwned.Id = Data_Car.Id) WHERE EXISTS (SELECT 1 FROM _backup_DataCarVisibleOnlyIfOwned WHERE _backup_DataCarVisibleOnlyIfOwned.Id = Data_Car.Id);",
        ],
        SqlFeature.ClearNewTag =>
        [
            "UPDATE Profile0_Career_Garage SET HasCurrentOwnerViewedCar = (SELECT HasCurrentOwnerViewedCar FROM _backup_NewTags WHERE _backup_NewTags.CarId = Profile0_Career_Garage.CarId) WHERE EXISTS (SELECT 1 FROM _backup_NewTags WHERE _backup_NewTags.CarId = Profile0_Career_Garage.CarId);",
        ],
        SqlFeature.InstallFlags =>
        [
            "UPDATE Data_Car SET IsInstalled = (SELECT IsInstalled FROM _backup_DataCarIsInstalled WHERE _backup_DataCarIsInstalled.Id = Data_Car.Id) WHERE EXISTS (SELECT 1 FROM _backup_DataCarIsInstalled WHERE _backup_DataCarIsInstalled.Id = Data_Car.Id);",
            "UPDATE Data_Car SET IsPurchased = (SELECT IsPurchased FROM _backup_DataCarIsPurchased WHERE _backup_DataCarIsPurchased.Id = Data_Car.Id) WHERE EXISTS (SELECT 1 FROM _backup_DataCarIsPurchased WHERE _backup_DataCarIsPurchased.Id = Data_Car.Id);",
            "UPDATE Data_Car SET IsDrivable = (SELECT IsDrivable FROM _backup_DataCarIsDrivable WHERE _backup_DataCarIsDrivable.Id = Data_Car.Id) WHERE EXISTS (SELECT 1 FROM _backup_DataCarIsDrivable WHERE _backup_DataCarIsDrivable.Id = Data_Car.Id);",
        ],
        _ => [],
    };
}
