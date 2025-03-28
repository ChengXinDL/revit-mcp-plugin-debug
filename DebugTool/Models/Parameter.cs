using System.ComponentModel;

public class Parameter : INotifyPropertyChanged
{
    private string _key;
    private object _value;
    private string _dataType = "text"; // 默认类型

    public string Key
    {
        get => _key;
        set
        {
            if (_key != value)
            {
                _key = value;
                OnPropertyChanged(nameof(Key));
            }
        }
    }

    public object Value
    {
        get => _value;
        set
        {
            if (_value != value)
            {
                _value = value;
                OnPropertyChanged(nameof(Value));
            }
        }
    }

    public string DataType
    {
        get => _dataType;
        set
        {
            if (_dataType != value)
            {
                _dataType = value;
                OnPropertyChanged(nameof(DataType));
                // 当类型改变时，可能需要转换值或重置值
                ConvertValueToMatchType();
            }
        }
    }

    private void ConvertValueToMatchType()
    {
        // 根据新类型尝试转换值或设置默认值
        switch (_dataType)
        {
            case "number":
                if (_value != null && !(_value is decimal) && decimal.TryParse(_value.ToString(), out decimal numValue))
                    _value = numValue;
                else if (!(_value is decimal))
                    _value = 0m;
                break;
            case "boolean":
                if (_value != null && !(_value is bool) && bool.TryParse(_value.ToString(), out bool boolValue))
                    _value = boolValue;
                else if (!(_value is bool))
                    _value = false;
                break;
            case "null":
                _value = null;
                break;
            case "object":
                if (!(_value is Dictionary<string, object>))
                    _value = new Dictionary<string, object>();
                break;
            case "array":
                if (!(_value is List<object>))
                    _value = new List<object>();
                break;
            default: // text
                if (_value != null && !(_value is string))
                    _value = _value.ToString();
                break;
        }
        OnPropertyChanged(nameof(Value));
    }

    // 返回转换为正确类型的值
    public object GetTypedValue()
    {
        return Value; // 因为我们在设置类型时已经转换了值
    }

    public Parameter(string key, object value, string dataType = "text")
    {
        _key = key;
        _dataType = dataType;
        // 设置值之前，先设置好类型
        _value = value;
        // 确保值匹配类型
        ConvertValueToMatchType();
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
