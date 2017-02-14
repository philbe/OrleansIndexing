using System.Web.Mvc;

namespace Orleans.Benchmarks.Conductor.Webrole{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new ErrorHandler.AiHandleErrorAttribute());
        }
    }
}