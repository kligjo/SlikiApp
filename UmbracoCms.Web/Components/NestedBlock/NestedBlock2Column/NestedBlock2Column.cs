using Umbraco.Cms.Core.Models.Blocks;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace UmbracoCms.Web.Components.NestedBlock;

public class NestedBlock2Column : NestedBlock
{
    public required BlockListItem LeftBlock { get; set; }
    public required BlockListItem RightBlock { get; set; }

    protected override object? ProcessBlock(IPublishedElement block)
    {
        if (block is not Umbraco.Cms.Web.Common.PublishedModels.NestedBlock2Column nestedBlock2Column
            || nestedBlock2Column.Columns == null || nestedBlock2Column.Columns.Count < 2) return null;
        if (nestedBlock2Column.Columns[0] == null || nestedBlock2Column.Columns[1] == null) return null;
        LeftBlock = nestedBlock2Column.Columns[0];
        RightBlock = nestedBlock2Column.Columns[1];
        return this;
    }
}
