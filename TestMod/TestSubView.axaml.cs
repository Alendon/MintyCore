﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MintyCore.Registries;
using MintyCore.UI;
using TestMod.Identifications;

namespace TestMod;

public partial class TestSubView : UserControl
{
    public TestSubView()
    {
        InitializeComponent();
    }

    [RegisterView("test_sub")]
    public static ViewDescription<TestSubView> Desc => new(ViewModelIDs.TestSub);
}