namespace Tevling.Shared;

public partial class Modal : ComponentBase
{
    [Parameter]
    public RenderFragment? HeaderContent { get; set; }

    [Parameter]
    public RenderFragment? BodyContent { get; set; }

    [Parameter]
    public RenderFragment? FooterContent { get; set; }

    [Parameter]
    public string? Id { get; set; }

    [Parameter]
    public string? Size { get; set; }
}