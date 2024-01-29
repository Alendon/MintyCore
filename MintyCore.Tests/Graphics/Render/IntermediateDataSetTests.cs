using MintyCore.Graphics.Render;
using MintyCore.Utils;

namespace MintyCore.Tests.Graphics.Render;

public class IntermediateDataSetTests
{
    private readonly Identification _dataId = new(1, 2, 3);
    private readonly IIntermediateDataManager _intermediateDataManagerStub = new Mock<IIntermediateDataManager>().Object;

    [Fact]
    public void GetSubData_WithId_ReturnValid()
    {
        var intermediateDataMock = new Mock<IntermediateData>();

        var dataSet = new IntermediateDataSet(_intermediateDataManagerStub,
            new Dictionary<Identification, IntermediateData>()
            {
                { _dataId, intermediateDataMock.Object }
            });
        
        dataSet.IncreaseUseCount();

        var result = dataSet.GetSubData(_dataId);
        result.Should().Be(intermediateDataMock.Object);
    }

    [Fact]
    public void GetSubData_NotExistingId_ThrowException()
    {
        var dataSet =
            new IntermediateDataSet(_intermediateDataManagerStub, new Dictionary<Identification, IntermediateData>());
        dataSet.IncreaseUseCount();

        dataSet.Invoking(x => x.GetSubData(_dataId))
            .Should().Throw<ArgumentException>().WithMessage("Intermediate data with id * not found");
    }

    [Fact]
    public void Reset_ResetShouldBeCalledOnAllSubData()
    {
        var intermediateDataMock = new Mock<IntermediateData>();

        var dataSet = new IntermediateDataSet(_intermediateDataManagerStub,
            new Dictionary<Identification, IntermediateData>()
            {
                { _dataId, intermediateDataMock.Object }
            });

        dataSet.Reset();

        intermediateDataMock.Verify(x => x.Reset(), Times.Once);
    }

    [Fact]
    public void Reset_SetUseCountToZero()
    {
        var dataSet =
            new IntermediateDataSet(_intermediateDataManagerStub, new Dictionary<Identification, IntermediateData>());

        dataSet.IncreaseUseCount();
        dataSet.Reset();

        dataSet.Invoking(x => x.DecreaseUseCount())
            .Should().Throw<InvalidOperationException>().WithMessage("Use count is already 0");
    }

    [Fact]
    public void IncreaseUseCount_NoException()
    {
        var dataSet =
            new IntermediateDataSet(_intermediateDataManagerStub, new Dictionary<Identification, IntermediateData>());

        Action act = () => dataSet.IncreaseUseCount();
        act.Should().NotThrow();
    }

    [Fact]
    public void DecreaseUseCount_AfterDecreaseToZero_ShouldReset()
    {
        var mock = new Mock<IntermediateData>();

        var dataSet = new IntermediateDataSet(_intermediateDataManagerStub,
            new Dictionary<Identification, IntermediateData>()
            {
                { _dataId, mock.Object }
            });

        dataSet.IncreaseUseCount();

        Action act = () => dataSet.DecreaseUseCount();
        act.Should().NotThrow();

        mock.Verify(x => x.Reset(), Times.Once);
    }

    [Fact]
    public void DecreaseUseCount_DecreaseToNegative_ShouldThrowException()
    {
        var dataSet =
            new IntermediateDataSet(_intermediateDataManagerStub, new Dictionary<Identification, IntermediateData>());

        dataSet.Invoking(x => x.DecreaseUseCount())
            .Should().Throw<InvalidOperationException>().WithMessage("Use count is already 0");
    }

    [Fact]
    public void CurrentAccessMode_SetAccessMode_ShouldBeSet()
    {
        var dataMock = new Mock<IntermediateData>();

        // ReSharper disable once UseObjectOrCollectionInitializer
        var dataSet = new IntermediateDataSet(_intermediateDataManagerStub,
            new Dictionary<Identification, IntermediateData>()
            {
                { _dataId, dataMock.Object }
            });


        dataSet.AccessMode = AccessMode.Read;


        dataMock.Object.AccessMode.Should().Be(AccessMode.Read);
    }
}