using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Elevator_Dispatching_System;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private ElevatorSystem _elevatorSystem;
    
    public MainWindow()
    {
        // 初始化窗口组件
        InitializeComponent();
        // 初始化电梯系统
        _elevatorSystem = new ElevatorSystem();
        // 设置窗口的DataContext
        this.DataContext = _elevatorSystem;
    }
}
