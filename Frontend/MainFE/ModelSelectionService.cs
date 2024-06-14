namespace MainFE;

using System;
using System.ComponentModel;

public class ModelSelectionService : INotifyPropertyChanged
{
    private string _selectedModel = "gemma:2b"; // Default model

    public string SelectedModel
    {
        get => _selectedModel;
        set
        {
            if (_selectedModel != value)
            {
                _selectedModel = value;
                OnPropertyChanged(nameof(SelectedModel));
            }
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    public void SetSelectedModel(string model)
    {
        SelectedModel = model;
        OnSelectedModelChange?.Invoke();
    }

    public event Action OnSelectedModelChange;

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}