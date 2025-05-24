using System.Windows.Input;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Elevator_Dispatching_System;

public class ElevatorTask
{
    public int _targetFloor {get;set;}
    public int _direction {get;set;}
    public int _assignedElevator {get;set;}
    
    public ElevatorTask(int floor, int direction, int elevator)
    {
        _targetFloor = floor;
        _direction = direction;
        _assignedElevator = elevator;
    }
}

public class ElevatorSystem : ObservableObject
{
    private Elevator[] _elevators;                        // 电梯系统包含的电梯集合
    private ObservableCollection<ElevatorTask> _tasks;    // 电梯系统的任务集合
    
    public RelayCommand<object> DispatchCommand { get; }  // 控制电梯调度的命令
    
    // 对外提供一个只读方法的电梯
    public IReadOnlyList<Elevator> Elevators => _elevators.AsReadOnly();
    
    public ElevatorSystem()
    {
        _elevators = new Elevator[5];
        for (int i = 0; i < _elevators.Length; i++)
        {
            _elevators[i] = new Elevator();
            _elevators[i].PropertyChanged += (s, e) => 
            {
                if (e.PropertyName == nameof(Elevator.CurrentFloor))
                    CheckTaskCompletion();
            };
        }
        _tasks = new ObservableCollection<ElevatorTask>();
        DispatchCommand = new RelayCommand<object>(Dispatch,canDispatch);
    }
    
    private void CheckTaskCompletion()
    {
        var completed = _tasks
            .Where(t => _elevators[t._assignedElevator].CurrentFloor == t._targetFloor ||
                        _elevators[t._assignedElevator].State == ElevatorState.Error).ToList();
        foreach (var task in completed)
        {
            _tasks.Remove(task);
        }
        DispatchCommand.RaiseCanExecuteChanged();
    }

    private int FindBestElevator(int targetFloor, int direction)
    {
        int bestElevator = -1;
        int minScore = int.MaxValue;
        for (int i = 0; i < _elevators.Length; i++)
        {
            // 获得当前电梯的状态参数
            var elevator = _elevators[i];
            int currentFloor = elevator.CurrentFloor;
            ElevatorState currentState = elevator.State;
            // 如果该电梯无法使用，那么直接跳过
            if (currentState == ElevatorState.Error)
                continue;
            // 如果有电梯在该楼层，那么直接分配
            if (currentFloor == targetFloor)
            {
                bestElevator = i;
                break;
            }
            // 使用分数来区分电梯的优先级
            int score = 0;
            // 如果想去的方向和电梯运行方向相同
            if ((direction==1 && currentState == ElevatorState.MovingUp) ||
                (direction==-1 && currentState == ElevatorState.MovingDown))
            {
                // 如果目标楼层就在当前路径上
                if ((direction == 1 && targetFloor >= currentFloor) ||
                    (direction == -1 && targetFloor <= currentFloor))
                {
                    score += Math.Abs(targetFloor - currentFloor) * 1;
                }
                else
                {
                    int furthestFloor = direction == 1 
                        ? elevator.FloorQueue.Max()  // 上行时取最大值
                        : elevator.FloorQueue.Min(); // 下行时取最小值
                    score += Math.Abs(furthestFloor - targetFloor) * 1;
                }
            }
            // 如果当前电梯是空闲状态
            else if (currentState == ElevatorState.Idle)
            {
                score += Math.Abs(targetFloor - currentFloor) * 1;
            }
            else
            {
                score += 20;
            }
            // 当前电梯需要执行的任务越多，优先级越低
            score += elevator.FloorQueue.Count * 3;
            // 计算当前电梯的分数是否为最低
            if (score < minScore)
            {
                minScore = score;
                bestElevator = i;
            }
        }
        return bestElevator;
    }
    
    private void Dispatch(object task)
    {
        int currfloor,direction;
        if (task is Object[] array)
        {
            currfloor = Convert.ToInt32(array[0]);
            direction = Convert.ToInt32(array[1]);
            int elevatorId = FindBestElevator(currfloor, direction);
            if (elevatorId >= 0)
            {
                var newtask = new ElevatorTask(currfloor, direction, elevatorId);
                _tasks.Add(newtask);
                _elevators[elevatorId].AddTask(currfloor.ToString());
                DispatchCommand.RaiseCanExecuteChanged();
            }
        }
    }

    private bool canDispatch(object task)
    {
        int currfloor,direction;
        if (task is Object[] array)
        {
            currfloor = Convert.ToInt32(array[0]);
            direction = Convert.ToInt32(array[1]);
            return !_tasks.Any(t => 
                t._targetFloor == currfloor && 
                t._direction == direction);
        }
        return false;
    }
}

public class RelayCommand<Type> : ICommand
{
    private readonly Action<Type> _execute;            // 命令可以触发的方法
    private readonly Func<Type,bool> _canExecute;      // 方法是否可以被触发
    public event EventHandler CanExecuteChanged;       // 提供方法来触发事件
    
    // 主要构造函数
    public RelayCommand(Action<Type> execute, Func<Type,bool> canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }
    
    // 对外提供的
    public bool CanExecute(object parameter)
    {
        if (_canExecute != null)
        {
            return _canExecute((Type)parameter);
        }
        return true; 
    }
    
    // 命令触发方法的逻辑
    public void Execute(object parameter)
    {
        if (_execute != null)
        {
            _execute.Invoke((Type)parameter);
        }
    }
    
    // 手动触发检查UI更新
    public void RaiseCanExecuteChanged()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
