using System;
using System.Collections.Generic;

namespace Minimal_Order_Api
{
    public interface IOrderAPI : IDisposable
    {
        IEnumerable<Order> GetOrderList(DateTime? startDate,
                                           decimal vatRateRegular,
                                           decimal vatRateReduced,
                                           out int totalNumberOfOrders,
                                           out int totalNumberOfPages,
                                           int page = 1,
                                           int pageSize = 50
        );

        /// <summary>Deserialize the stored access token.</summary>
        /// <param name="accessToken">The serialized access token</param>
        void DeserializeAccessToken(string accessToken);

        /// <summary>Serializes the current access token.</summary>
        /// <returns>The serialized access token. For example a JSON string.</returns>
        string SerializeAccessToken();
    }
}