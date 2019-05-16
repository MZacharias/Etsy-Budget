# EtsyBudget

This C# application facilitates OAuth connections to the Etsy API to create a custom monthly budget Excel worksheet.  Another intention of the project is to retrieve all fees into the budget to realize overall net profits.  These additional fees are not currently included in reports through the website.

### Configuration

Please update the following application settings (app.config) to work with the appropriate account:
```
  <appSettings>
    <add key="Consumer_Key" value="" />
    <add key="Consumer_Secret" value="" />
    <add key="Token" value="" />
    <add key="Token_Secret" value="" />
    <add key="Shop_Name" value="" />
  </appSettings>
```
 Use the following documentation to retreive these values from the Etsy website:
 https://www.etsy.com/developers/documentation/getting_started/api_basics
