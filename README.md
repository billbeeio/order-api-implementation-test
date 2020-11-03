Read the following document carefully.  
It contains important information about the task itself and how to submit your solution.

## Task overview

Create an implementation of the [IOrderAPI](./MinimalOrderApi/IOrderAPI.cs) to import orders from the [Shopify ReST API](https://shopify.dev/docs/admin-api/rest/).

Priorities:
- The Implementation can be used with any access token (no hardcoded strings etc.)
- If possible: All fields in the models should be filled by the implementation
- Calculated prices are correct
- Tax is correct calculated
- Discounts (per position) should set to the `OrderItem.Discount` property.
- The calculated total of the order must be equal to the `Order.TotalCost` (gross) which comes from the external system.
- Don't add any NuGet Packages
- Use the [MinimalOrderApi/RestClientBaseClass.cs](MinimalOrderApi/RestClientBaseClass.cs) to consume the ReST API

### `IOrderAPI.GetOrderList`
This method is used for querying a list of orders which are created or modified after the the `startDate`.
`vatRateRegular` and `vatRateReduced` are the default vat rates of the client. You can use this for calculating the tax amount
if the API doesn't provide this information.
you also need those to specify `Order.TaxRateRegular`, `Order.TaxRateReduced` and `OrderItem.TaxIndex`.

Possible values for the tax index:
- 0 = No vat applicable
- 1 = Regular vat
- 2 = Reduced vat.

`totalNumberOfOrders` and `totalNumberOfPages`: This parameters are used for pagination. Set them to the corresponding values.

### `IOrderAPI.DeserializeAccessToken` and `IOrderAPI.SerializeAccessToken`
`IOrderAPI.DeserializeAccessToken` is called by the client before `IOrderAPI.GetOrderList`. 
Use this to setup your implementation for any api related authorization.

`IOrderAPI.SerializeAccessToken` is called by the client to retrieve the current access token. Return a new token if the access token can expire.

## How to start
1. Download the source of this repository
   ```bash
   $ git clone https://github.com/billbeeio/order-api-implementation-test.git
   $ cd order-api-implementation-test
   $ git remote remove origin
   $ git checkout -b my-implementation
   ```
2. Create a implementation class `{NameOfTheImplementation}Api` inside the [`Billbee.MinimalOrderApi.Implementation`](./MinimalOrderApi/Implementation) namespace.
3. Edit the [Program.cs](./MinimalOrderApi/Program.cs) and set the variables:
   ```C#
   IOrderAPI api = null; // Your implementation
   string accessToken = null; // Your access token
   ```
4. Add the logic to your implementation.
5. Run the console application to test your implementation.

### HTTP Interaction
To interact with a HTTP service, your implementation should derive from the [RestClientBaseClass](./MinimalOrderApi/RestClientBaseClass.cs).

## Submit your solution
1. Commit your changes using `git commit`
2. Create a zip of the changed files using the following command in the git bash:
   ```bash
   $ git archive --format=zip HEAD `git diff master HEAD --name-only` > `git branch --show-current`.zip
   ````
3. Send the generated zip file (named as `{BranchName}.zip`) attached to a mail to your contact at Billbee.
