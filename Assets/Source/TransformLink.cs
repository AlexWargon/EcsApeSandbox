using Wargon.Ecsape;

public class TransformLink : ComponentLink<TransformReference> {
    public override void Link(ref Entity entity) {
        var translation = new Translation {
            position = value.value.position,
            scale = value.value.localScale,
            rotation = value.value.rotation
        };
        entity.Add(value);
        entity.Add(translation);
    }
}