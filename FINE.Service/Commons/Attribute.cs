namespace FINE.Service.Commons
{
    public class BooleanAttribute : Attribute
    {
    }

    public class DateRangeAttribute : Attribute
    {
    }

    public class IntAttribute : Attribute
    {
    }
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class SkipAttribute : Attribute
    {
    }
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ChildAttribute : Attribute
    {
    }
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ExcludeAttribute : Attribute
    {
        public string Field { get; set; }
    }
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ContainAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class HiddenParamsAttribute : System.Attribute
    {
        public string Params { get; set; }

        public HiddenParamsAttribute(string parameters)
        {
            this.Params = parameters;
        }
    }
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class HiddenControllerAttribute : System.Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class StringAttribute : System.Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class ImmutableAttribute : System.Attribute
    {
    }
    public class SortAttribute : System.Attribute
    {
    }
}