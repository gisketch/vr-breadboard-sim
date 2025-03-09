public interface IComponent
{
    void OnNodeHover(Node node);
    void OnNodeClick(Node node);
}

public interface IComponentTool
{
    void Activate();
    void Deactivate();
    void OnNodeHover(Node node);
    void OnNodeClick(Node node);
    void UpdateColors();
}
