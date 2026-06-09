using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Web.Common.PublishedModels;

namespace UmbracoCms.Web.Components;

public partial class LanguageSwitch : ViewComponentExtended
{
    public List<Language> Languages { get; init; } = [];

    public IViewComponentResult Invoke()
    {
        PageHome? homePage = NodeProvider.HomePage;
        if (homePage is not { Cultures.Count: > 1 }) return Content("");

        foreach ((string culture, _) in homePage.Cultures)
        {
            CultureInfo infoCulture = new(culture);
            CultureInfo neutralCulture = infoCulture.IsNeutralCulture ? infoCulture : infoCulture.Parent;

            Language language = new()
            {
                Label = infoCulture.TextInfo.ToTitleCase(neutralCulture.NativeName),
                LanguageCode = infoCulture.TwoLetterISOLanguageName,
                CultureCode = culture,
                Url = NodeProvider.GetCurrentNode()?.Url(culture) is { } url and not "#" ? url : homePage.Url(culture),
                Icon = $"/assets/svg/flags/{culture}.svg",
                IsActive = infoCulture.Equals(CultureInfo.CurrentCulture),
            };
            Languages.Add(language);
        }

        return View("LanguageSwitch", this);
    }
}
