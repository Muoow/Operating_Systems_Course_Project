using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;

namespace Elevator_Dispatching_System;

public enum ElevatorState
{
    Idle,
    MovingUp,
    MovingDown,
    Opening,
    Closing,
    Error,
}

public class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
 
    protected void RaisePropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class Elevator : ObservableObject
{
    // 当前状态和任务列表
    private int _currentFloor;                      // 电梯目前所在楼层
    private ElevatorState _state;                   // 电梯当前的状态（空闲/上行/下行...）
    private ObservableCollection<int> _floorQueue;  // 当前任务列表
    // 任务调度参数
    private CancellationTokenSource _cts;           // 
    private readonly object _queueLock;             // 限制线程访问代码的线程锁
    private bool _isProcessingQueue;                // 当前是否有任务在运行
    // 调度电梯的命令
    public RelayCommand<string> AddFloorCommand { get; }        // 向任务列表添加任务的命令
    public RelayCommand<object> OpenCommand { get; }            // 控制电梯开门的命令
    public RelayCommand<object> CloseCommand { get; }           // 控制电梯关门的命令
    public RelayCommand<object> EmergencyCommand { get; }       // 控制电梯紧急情况的命令
    
    public int CurrentFloor
    {
        get => _currentFloor;  
        set
        { 
            if (_currentFloor != value) 
            {
                _currentFloor = value;
                RaisePropertyChanged();
            }
        }
    }
    
    public ElevatorState State
    {
        get => _state;
        set 
        {
            if (_state != value) 
            {
                _state = value;
                RaisePropertyChanged();
            }
        }
    }

    public IReadOnlyList<int> FloorQueue => _floorQueue.AsReadOnly();
    
    public Elevator()
    {
        // 状态初始化
        CurrentFloor = 1;
        State = ElevatorState.Idle;
        // 初始化任务列表
        _floorQueue = new ObservableCollection<int>();
        _floorQueue.CollectionChanged += FloorQueue_CollectionChanged;
        // 调度参数初始化
        _cts = new CancellationTokenSource();
        _queueLock = new object();
        _isProcessingQueue = false;
        // 命令初始化
        AddFloorCommand = new RelayCommand<string>(AddTask,CanAddTask);
        EmergencyCommand = new RelayCommand<object>(EmergencyPause,canEmergencyPause);
        OpenCommand = new RelayCommand<object>(OpenDoor,canOpenDoor);
        CloseCommand = new RelayCommand<object>(CloseDoor,canCloseDoor);
    }

    private async void FloorQueue_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (State != ElevatorState.Error && !_isProcessingQueue && _floorQueue.Any())
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            await ProcessQueue(_cts.Token);
        }
    }
    
    public void AddTask(string targetFloor)
    {
        int floor = int.Parse(targetFloor);
        lock (_queueLock)
        {
            if (!_floorQueue.Contains(floor)) 
            {
                SortQueue(floor);
                AddFloorCommand.RaiseCanExecuteChanged();
            }
        }
    }
    
    private bool CanAddTask(string targetFloor)
    {
        int floor = int.Parse(targetFloor);
        return floor >= 1 && floor <= 20 && !_floorQueue.Contains(floor);
    }

    private void SortQueue(int floor)
    {
        bool isAdded = false;
        for (int i = 0; i < _floorQueue.Count; i++) 
        {
            int targetFloor = _floorQueue[i];
            if (State == ElevatorState.MovingUp) 
            {
                if (targetFloor < CurrentFloor) 
                {
                    if (floor > targetFloor) {
                        _floorQueue.Insert(i, floor);
                        isAdded = true;
                        break;
                    }
                    else{
                        continue;
                    }
                }
                else {
                    if (floor < targetFloor && floor > CurrentFloor) {
                        _floorQueue.Insert(i, floor);
                        isAdded = true;
                        break;
                    }
                    else{
                        continue;
                    }
                }
            }
            else if (State == ElevatorState.MovingDown) 
            {
                if (targetFloor > CurrentFloor)
                {
                    if (floor < targetFloor)
                    {
                        _floorQueue.Insert(i, floor);
                        isAdded = true;
                        break;
                    }
                    else
                    {
                        continue;
                    }
                }
                else
                {
                    if (floor > targetFloor && floor < CurrentFloor)
                    {
                        _floorQueue.Insert(i, floor);
                        isAdded = true;
                        break;
                    }
                    else
                    {
                        continue;
                    }
                }
            }
        }
        if(!isAdded) 
        {
            _floorQueue.Add(floor);
        }
    }

    private async Task ProcessQueue(CancellationToken token)
    {
        if(_isProcessingQueue || State == ElevatorState.Error)
            return;
        _isProcessingQueue = true;
        try
        {
            while (_floorQueue.Any() && !token.IsCancellationRequested)
            {
                int nextFloor;
                lock (_queueLock)
                {
                    nextFloor = _floorQueue[0];
                }
                await MoveToFloor(nextFloor);
            }
            if (_floorQueue.Count == 0 && !token.IsCancellationRequested)
            {
                State= ElevatorState.Idle;
                _isProcessingQueue = false;
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during cancellation
        }
    }
    
    private async Task MoveToFloor(int targetFloor)
    {
        if (targetFloor == CurrentFloor) { return; }
        // 执行移动操作
        await Moving(targetFloor,1000 ,_cts.Token);
        // 执行开关电梯门操作
        await OpenAndClose(1000, 1000 ,_cts.Token);
    }

    private async Task OpenAndClose(int timeToOpen, int timeToClose, CancellationToken token)
    {
        try
        {
            if (State == ElevatorState.Error) return;
            State = ElevatorState.Opening;
            await Task.Delay(timeToOpen, token);
            if (State == ElevatorState.Error) return;
            State = ElevatorState.Closing;
            await Task.Delay(timeToClose, token);
        }
        catch (OperationCanceledException)
        {
            // Expected during cancellation
        }
    }
    
    private async Task Moving(int targetFloor, int timePerFloor, CancellationToken token)
    {
        try
        {
            int direction = targetFloor > CurrentFloor ? 1 : -1;
            State = targetFloor > CurrentFloor ? ElevatorState.MovingUp : ElevatorState.MovingDown;
            while (CurrentFloor != targetFloor && !token.IsCancellationRequested) 
            {
                if (State == ElevatorState.Error) break;
                await Task.Delay(timePerFloor, token);
                CurrentFloor += direction;
                // 每到达一层楼检测任务列表中是否有该任务
                bool shouldStop = false;
                lock (_queueLock)
                {
                    if (CurrentFloor!=targetFloor && _floorQueue.Contains(CurrentFloor))
                    {
                        shouldStop = true;
                        _floorQueue.Remove(CurrentFloor);
                        AddFloorCommand.RaiseCanExecuteChanged();
                    }
                }
                // 如果有，那就接一次客并删除该任务
                if (shouldStop) 
                {
                    await OpenAndClose(1000, 1000, token);
                    State = targetFloor > CurrentFloor ? ElevatorState.MovingUp : ElevatorState.MovingDown;
                }
            }
            // 到达目标楼层后删除任务并手动更新UI
            _floorQueue.Remove(targetFloor);
            AddFloorCommand.RaiseCanExecuteChanged();
        }
        catch (OperationCanceledException)
        {
            // Expected during cancellation
        }
    }

    public void OpenDoor(object obj)
    {
        if (State == ElevatorState.Idle || State == ElevatorState.Closing)
        {
            State = ElevatorState.Opening;
            Task.Delay(3000).ContinueWith( _ => 
            {
                if (State == ElevatorState.Opening) 
                {
                    CloseDoor(null);
                }
            });
        }
    }

    public bool canOpenDoor(object obj)
    {
        return State != ElevatorState.Error;
    }
    
    public void CloseDoor(object obj)
    {
        if (State == ElevatorState.Opening)
        {
            State = ElevatorState.Closing;
            Task.Delay(1000).ContinueWith(_ => 
            {
                if (State == ElevatorState.Closing)
                {
                    State = ElevatorState.Idle;
                }
            });
        }
    }
    
    public bool canCloseDoor(object obj)
    {
        return State != ElevatorState.Error;
    }
    
    public void EmergencyPause(object obj)
    {
        State = ElevatorState.Error;
        _cts?.Cancel();
        lock (_queueLock)
        {
            _floorQueue.Clear();
            for (int i = 1; i <= 20; i++)
            {
                _floorQueue.Add(i);
            }
            // 手动更新UI
            AddFloorCommand.RaiseCanExecuteChanged();
            EmergencyCommand.RaiseCanExecuteChanged();
            OpenCommand.RaiseCanExecuteChanged();
            CloseCommand.RaiseCanExecuteChanged();
        }
    }

    public bool canEmergencyPause(object obj)
    {
        return State != ElevatorState.Error;
    }
}
