using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace BusTrips.Web.TagHelpers
{
    [HtmlTargetElement("label", Attributes = "asp-for")]
    public class RequiredLabelTagHelper : TagHelper
    {
        [HtmlAttributeName("asp-for")]
        public ModelExpression For { get; set; }

        // Check if the property has a [Required] attribute and append an asterisk if it does
        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            var isRequired = For.Metadata
                .ContainerType?
                .GetProperty(For.Metadata.PropertyName!)?
                .GetCustomAttributes(typeof(RequiredAttribute), false)
                .Any() ?? false;

            if (isRequired)
            {
                output.Content.AppendHtml(" <sup class='required-asterisk'>*</sup>");
            }
        }
    }
}
