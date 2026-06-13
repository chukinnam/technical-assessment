namespace CourseInquiryApi.Services.Crm;


public class CrmDispatchResult
{
    public bool Success { get; set; }
    public string? ExternalId { get; set; }
    public CrmError? Error { get; set; }

    public CrmDispatchResult(bool success, string? externalId)
    {
        this.Success = success;
        this.ExternalId = externalId;
    }

   

    public static CrmDispatchResult Ok(string externalId) {
        return new CrmDispatchResult(true, externalId);
    }
    public static CrmDispatchResult Fail(CrmError error) {
        return new CrmDispatchResult(false, null) { Error = error };
    }
}

public class CrmError
{
    public string? Code { get; set; }
    public string? Message { get; set; }
    public bool IsTransient { get; set; } = true;
    public CrmError(string code,string message,bool isTransient) {
        this.Code = code;
        this.Message = message;
        this.IsTransient = isTransient;
    }
   
}
