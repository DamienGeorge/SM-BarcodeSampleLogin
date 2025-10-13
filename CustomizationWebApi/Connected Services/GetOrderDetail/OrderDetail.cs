

namespace CustomizationWebApi.Connected_Services.GetOrderDetail
{
    [Serializable]
    public class Rootobject
    {
        public Maintenanceorder MaintenanceOrder { get; set; }
    }

    [Serializable]
    public class Maintenanceorder
    {
        public Maintenanceordertype MaintenanceOrderType { get; set; }
    }

    [Serializable]
    public class Maintenanceordertype
    {
        public string MaintenanceOrderDesc { get; set; }
        public string MaintenanceOrderType { get; set; }
        public string MaintenanceOrder { get; set; }
        public string MaintenancePlannerGroup { get; set; }
        public string MainWorkCenter { get; set; }
        public To_Maintenanceorderlongtext to_MaintenanceOrderLongText { get; set; }
        public string MaintenanceActivityType { get; set; }
        public string UserStatusText { get; set; }
        public To_Maintenanceorderpartner to_MaintenanceOrderPartner { get; set; }
        public To_Maintenanceorderoperation to_MaintenanceOrderOperation { get; set; }
        public string SystemStatusText { get; set; }
    }

    [Serializable]
    public class To_Maintenanceorderlongtext
    {
        public Maintenanceorderlongtext_Type MaintenanceOrderLongText_Type { get; set; }
    }

    [Serializable]
    public class Maintenanceorderlongtext_Type
    {
        public string Language { get; set; }
        public string MaintenanceOrderLongText { get; set; }
    }

    [Serializable]
    public class To_Maintenanceorderpartner
    {
        public Maintenanceorderpartner_Type[] MaintenanceOrderPartner_Type { get; set; }
    }

    [Serializable]
    public class Maintenanceorderpartner_Type
    {
        public string PartnerFunction { get; set; }
        public string MaintenanceOrderPartner { get; set; }
    }

    [Serializable]
    public class To_Maintenanceorderoperation
    {
        public Maintenanceorderoperationtype[] MaintenanceOrderOperationType { get; set; }
    }

    [Serializable]
    public class Maintenanceorderoperationtype
    {
        public string MaintOrdOperationDurationUnit { get; set; }
        public string MaintenanceOrderSubOperation { get; set; }
        public string MaintOrderOperationQuantity { get; set; }
        public string MaintOrdOperationWorkDuration { get; set; }
        public string ActivityType { get; set; }
        public string MaintOrdOperationQuantityUnit { get; set; }
        public string UserStatusText { get; set; }
        public string OperationDescription { get; set; }
        public string MaintOrdOperationExecutionRate { get; set; }
        public string ActualWorkQuantity { get; set; }
        public string MaintOrderOperationDuration { get; set; }
        public string Plant { get; set; }
        public string MaintenanceOrder { get; set; }
        public string ForecastWorkQuantity { get; set; }
        public string OperationControlKey { get; set; }
        public string WorkCenter { get; set; }
        public string MaintOrdOpWorkDurationUnit { get; set; }
        public string MaintenanceOrderOperation { get; set; }
        public string SystemStatusText { get; set; }
    }
}
