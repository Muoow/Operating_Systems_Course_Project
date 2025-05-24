using System.Windows.Data;
using System.Globalization;
using System.Windows.Media;

namespace Elevator_Dispatching_System;

public class StateToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        switch (value)
        {
            case ElevatorState.Idle:
                return Brushes.LightGray;
            case ElevatorState.MovingUp:
                return Brushes.LightBlue;
            case ElevatorState.MovingDown:
                return Brushes.LightYellow;
            case ElevatorState.Opening:
                return Brushes.LightGreen;
            case ElevatorState.Closing:
                return Brushes.LightSalmon;
            case ElevatorState.Error:
                return Brushes.Red;
            default:
                return Brushes.LightGray;
        }
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class StateToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        switch (value)
        {
            case ElevatorState.Idle:
                return "空 闲 中";
            case ElevatorState.MovingUp:
                return "正在上行";
            case ElevatorState.MovingDown:
                return "正在下行";
            case ElevatorState.Opening:
                return "开 门 中";
            case ElevatorState.Closing:
                return "关 门 中";
            case ElevatorState.Error:
                return "电梯故障！！！";
            default:
                return "空 闲 中";
        }
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class FloorToRowConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int floor)
        {
            return 20 - floor;
        }
        return 19;
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

internal class MultiValueConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        return values.ToArray();
    }
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}