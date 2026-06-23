using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using UmbracoCms.Web.Infrastructure.DocumentTypes;

namespace UmbracoCms.Web.Infrastructure.NotificationHandlers;

/// <summary>
/// Creates the authentication page document type and content on application startup.
/// </summary>
public class AuthPageContentNotificationHandler : INotificationHandler<UmbracoApplicationStartedNotification>
{
    private readonly IContentTypeService _contentTypeService;
    private readonly IContentService _contentService;
    private readonly IDataTypeService _dataTypeService;
    private readonly IFileService _fileService;
    private readonly IShortStringHelper _shortStringHelper;
    private readonly ILogger<AuthPageContentNotificationHandler> _logger;

    public AuthPageContentNotificationHandler(
        IContentTypeService contentTypeService,
        IContentService contentService,
        IDataTypeService dataTypeService,
        IFileService fileService,
        IShortStringHelper shortStringHelper,
        ILogger<AuthPageContentNotificationHandler> logger)
    {
        _contentTypeService = contentTypeService;
        _contentService = contentService;
        _dataTypeService = dataTypeService;
        _fileService = fileService;
        _shortStringHelper = shortStringHelper;
        _logger = logger;
    }

    public void Handle(UmbracoApplicationStartedNotification notification)
    {
        // Ensure the document type exists
        EnsureDocumentType();
        
        // Ensure the auth page content exists
        EnsureAuthPageContent();
    }

    private void EnsureDocumentType()
    {
        var contentType = _contentTypeService.Get(PageAuthConstants.Alias);
        if (contentType != null)
        {
            return;
        }

        _logger.LogInformation("Creating Authentication Page document type.");

        contentType = new ContentType(_shortStringHelper, -1)
        {
            Alias = PageAuthConstants.Alias,
            Name = PageAuthConstants.Name,
            Icon = PageAuthConstants.Icon,
            Description = PageAuthConstants.Description,
            AllowedAsRoot = true
        };

        // Get the data types by editor alias (reliable regardless of display name)
        var textboxDataType = _dataTypeService.GetByEditorAlias("Umbraco.TextBox").FirstOrDefault();
        var textareaDataType = _dataTypeService.GetByEditorAlias("Umbraco.TextArea").FirstOrDefault();

        if (textboxDataType == null || textareaDataType == null)
        {
            _logger.LogError("Required data types (Umbraco.TextBox / Umbraco.TextArea) not found. Cannot create Authentication Page document type.");
            return;
        }

        // Title property
        var titlePropertyType = new PropertyType(_shortStringHelper, textboxDataType)
        {
            Alias = PageAuthConstants.TitleAlias,
            Name = "Title",
            Description = "The page title displayed to users",
            Mandatory = false
        };
        contentType.AddPropertyType(titlePropertyType, "Content");

        // Intro property
        var introPropertyType = new PropertyType(_shortStringHelper, textareaDataType)
        {
            Alias = PageAuthConstants.IntroAlias,
            Name = "Intro",
            Description = "The introductory text explaining the authentication",
            Mandatory = false
        };
        contentType.AddPropertyType(introPropertyType, "Content");

        // Submit Button Text property
        var submitButtonPropertyType = new PropertyType(_shortStringHelper, textboxDataType)
        {
            Alias = PageAuthConstants.SubmitButtonTextAlias,
            Name = "Submit Button Text",
            Description = "The text for the submit button",
            Mandatory = false
        };
        contentType.AddPropertyType(submitButtonPropertyType, "Content");

        // Assign the PageAuth template so Umbraco knows how to render this document type
        var template = _fileService.GetTemplate(PageAuthConstants.TemplateAlias);
        if (template != null)
        {
            contentType.AllowedTemplates = new[] { template };
            contentType.SetDefaultTemplate(template);
        }
        else
        {
            _logger.LogWarning("PageAuth template not found. The authentication page may not render correctly.");
        }

        _contentTypeService.Save(contentType);
        _logger.LogInformation("Authentication Page document type created successfully.");
    }

    private void EnsureAuthPageContent()
    {
        var authPage = _contentService
            .GetRootContent()
            .FirstOrDefault(content => content.ContentType.Alias == PageAuthConstants.Alias);

        if (authPage != null)
        {
            // Migrate: if previously created with the old name "Login" (URL /login), rename to
            // "Auth" so its URL becomes /auth, matching DefaultAuthPagePath in the middleware.
            bool needsSave = false;
            if (string.Equals(authPage.Name, "Login", StringComparison.OrdinalIgnoreCase))
            {
                authPage.Name = "Auth";
                needsSave = true;
            }

            if (authPage.TemplateId == null || authPage.TemplateId == 0)
            {
                var template = _fileService.GetTemplate(PageAuthConstants.TemplateAlias);
                if (template != null)
                {
                    authPage.TemplateId = template.Id;
                    needsSave = true;
                }
            }

            if (needsSave)
            {
                var migrateResult = _contentService.Save(authPage);
                if (migrateResult.Success)
                {
                    // Non-culture-variant content: publish without specifying cultures
                    _contentService.Publish(authPage, []);
                }
                _logger.LogInformation("Authentication Page content migrated successfully.");
            }

            return;
        }

        _logger.LogInformation("Creating Authentication Page content.");

        // Name "Auth" so Umbraco generates the URL /auth, matching DefaultAuthPagePath in the middleware.
        // pageAuth is NOT culture-variant: use invariant Name and SetValue (no culture parameter).
        authPage = _contentService.Create("Auth", Constants.System.Root, PageAuthConstants.Alias);
        authPage.SetValue(PageAuthConstants.TitleAlias, "Access Required");
        authPage.SetValue(PageAuthConstants.IntroAlias, "Please enter your access code to continue.");
        authPage.SetValue(PageAuthConstants.SubmitButtonTextAlias, "Submit");

        // Assign the template so this content node can be rendered
        var pageTemplate = _fileService.GetTemplate(PageAuthConstants.TemplateAlias);
        if (pageTemplate != null)
        {
            authPage.TemplateId = pageTemplate.Id;
        }

        var saveResult = _contentService.Save(authPage);
        if (saveResult.Success)
        {
            // Non-culture-variant: publish with empty cultures array
            var publishResult = _contentService.Publish(authPage, []);
            if (publishResult.Success)
            {
                _logger.LogInformation("Authentication Page content created and published successfully.");
            }
            else
            {
                _logger.LogWarning("Failed to publish the Authentication Page content: {Reason}", publishResult.Result);
            }
        }
        else
        {
            _logger.LogWarning("Failed to save the Authentication Page content.");
        }
    }
}