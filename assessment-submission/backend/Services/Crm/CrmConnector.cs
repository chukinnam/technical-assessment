using CourseInquiryApi.Models;

namespace CourseInquiryApi.Services.Crm;

public class CrmConnector : ICrmConnector
{
    private readonly bool successSyncCrm = true; // set to false to simulate CRM rejecting the record (permanent failure)

    public async Task<CrmDispatchResult> SendInquiryAsync(Inquiry inquiry, CancellationToken ct)
    {

        //simulate fail crm sync throw erro and return 
        if (!successSyncCrm)
        {

            return CrmDispatchResult.Fail(
                new CrmError("INVALID_PAYLOAD", "CRM rejected the record.",  false));
        }


        //simulate crm return a externalId to me
        var externalId = $"CRM-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
        return CrmDispatchResult.Ok(externalId);
    }
}
