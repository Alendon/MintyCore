using MintyCore.Graphics.Render.Data;
using MintyCore.Graphics.Render.Managers;
using MintyCore.Utils;

namespace MintyCore.Tests.Graphics.Render;

public class IntermediateDataTests
{
    private readonly Mock<IIntermediateDataManager> _intermediateDataManager = new();

    private readonly Identification _intermediateDataId = new(1, 2, 3);


    [Fact]
    public void DecreaseRefCount_ToZero_ShouldRecycleTheInstance()
    {
        var intermediateData = new Mock<IntermediateDataMockable>(_intermediateDataManager.Object);
        intermediateData.SetupGet(x => x.Identification).Returns(_intermediateDataId);
        
        intermediateData.Object.IncreaseRefCount();
        intermediateData.Object.DecreaseRefCount();

        _intermediateDataManager.Verify(x => x.RecycleIntermediateData(_intermediateDataId, intermediateData.Object),
            Times.Once);
    }

    [Fact]
    public void MoreDecreaseRefCountsThanIncrease_ShouldThrowException()
    {
        var intermediateData = new Mock<IntermediateDataMockable>(_intermediateDataManager.Object);
        intermediateData.SetupGet(x => x.Identification).Returns(_intermediateDataId);
        
        intermediateData.Object.IncreaseRefCount();
        
        var act = () => intermediateData.Object.DecreaseRefCount();

        act.Should().NotThrow();
        act.Should().Throw<InvalidOperationException>().WithMessage("The ref count can not be decreased below 0");
    }
    
    // ReSharper disable once MemberCanBePrivate.Global
    public abstract class IntermediateDataMockable : IntermediateData
    {
        // ReSharper disable once PublicConstructorInAbstractClass
        public IntermediateDataMockable(IIntermediateDataManager intermediateDataManager)
        {
            IntermediateDataManager = intermediateDataManager;
        }
    }
}