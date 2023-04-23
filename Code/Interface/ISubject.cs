
using System;

public interface ISubject
{
    void RegisterObserver(IObserver observer);
    void RemoveObserver(IObserver observer);
    void NotifyObserver(IObserver observer, ObserverPlayerType type);
    void NotifyObservers<T>(T valueChanged, ObserverPlayerType type);
}
