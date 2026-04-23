using System;
using System.Windows.Forms;
using RunnerGame.Presentation;
using RunnerGame.Rendering;

namespace RunnerGame
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            ApplicationConfiguration.Initialize();

            using var form = new GameForm();
            using var renderer = new GameRenderer();
            _ = new GamePresenter(form, renderer);

            Application.Run(form);
        }
    }
}
