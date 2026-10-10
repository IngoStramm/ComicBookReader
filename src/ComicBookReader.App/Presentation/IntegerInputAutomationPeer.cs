using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;

namespace ComicBookReader.App.Presentation;

internal sealed class IntegerInputAutomationPeer : FrameworkElementAutomationPeer, IRangeValueProvider
{
    private readonly IntegerInput owner;
    private readonly int minimum;
    private readonly int maximum;

    public IntegerInputAutomationPeer(IntegerInput owner, int minimum, int maximum) : base(owner)
    {
        this.owner = owner;
        this.minimum = minimum;
        this.maximum = maximum;
    }

    double IRangeValueProvider.Value => owner.Value;
    double IRangeValueProvider.Minimum => minimum;
    double IRangeValueProvider.Maximum => maximum;
    double IRangeValueProvider.SmallChange => 1;
    double IRangeValueProvider.LargeChange => 10;
    bool IRangeValueProvider.IsReadOnly => false;

    void IRangeValueProvider.SetValue(double value)
    {
        if (!owner.IsEnabled)
            throw new ElementNotEnabledException();
        if (!double.IsFinite(value) || Math.Truncate(value) != value || value < minimum || value > maximum)
            throw new ArgumentOutOfRangeException(nameof(value), "Value must be a whole number within the control's range.");

        owner.Value = (int)value;
    }

    public override object? GetPattern(PatternInterface patternInterface) =>
        patternInterface == PatternInterface.RangeValue ? this : base.GetPattern(patternInterface);

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Spinner;

    protected override bool IsEnabledCore() => owner.IsEnabled && base.IsEnabledCore();

    protected override string GetClassNameCore() => nameof(IntegerInput);

    protected override string GetNameCore()
    {
        string name = base.GetNameCore();
        if (string.IsNullOrWhiteSpace(name))
            name = AutomationProperties.GetName(owner.Editor);
        return string.IsNullOrWhiteSpace(name) ? "Integer input" : name;
    }
}
