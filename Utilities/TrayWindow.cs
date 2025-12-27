using System.Windows;

public abstract class TrayWindow : Window
{
    //Funciona como un override para que, al cerrar una ventana del tray, no se cierre el programa. 
    protected TrayWindow()
    {
        Closing += (s, e) =>
        {
            e.Cancel = true;
            Hide();
        };
    }
}