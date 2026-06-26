#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.NetLogic;
using FTOptix.NativeUI;
using FTOptix.HMIProject;
using FTOptix.UI;
using FTOptix.CoreBase;
using FTOptix.Core;
using System.Collections.Generic;
using FTOptix.Recipe;
using FTOptix.System;
using FTOptix.S7TiaProfinet;
using FTOptix.S7TCP;
#endregion

public class CalendarLogic_R0 : BaseNetLogic
{
    private Dictionary<int, string> monthDictionary = new Dictionary<int, string>(){
            { 1, "Jan" },
            { 2, "Feb" },
            { 3, "Mar" },
            { 4, "Apr" },
            { 5, "May" },
            { 6, "Jun" },
            { 7, "Jul" },
            { 8, "Aug" },
            { 9, "Sep" },
            { 10, "Oct" },
            { 11, "Nov" },
            { 12, "Dec" }
        };

    private EditableLabel yearLabel;
    private Label monthLabel;
    private DateTime dt;
    private IUANode selectedDate;

    public override void Start()
    {
        yearLabel = Owner.Get<EditableLabel>("Year");
        monthLabel = Owner.Get<Label>("Month");
        selectedDate = LogicObject.GetAlias("AliasDate");
        dt = ((IUAVariable)selectedDate).Value;
        RefreshLabels();
    }

    public override void Stop()
    {
        // Insert code to be executed when the user-defined logic is stopped
    }

    private void RefreshLabels() {
        yearLabel.Text = dt.Year.ToString();
        monthLabel.Text = monthDictionary[dt.Month];
        UpdateDays();
    }

    [ExportMethod]
    public void YearUpDown(bool up)
    {
        if (up)
            dt = dt.AddYears(1);
        else
            dt = dt.AddYears(-1);

        RefreshLabels();
    }

    [ExportMethod]
    public void MonthUpDown(bool up)
    {
        if (up)
            dt = dt.AddMonths(1);
        else
            dt = dt.AddMonths(-1);

        RefreshLabels();
    }

    private void UpdateDays()
    {
        int startWeekDay = (int)(dt.AddDays(-dt.Day + 1)).DayOfWeek;
        if (startWeekDay == 0)
            startWeekDay = 7;
        int maxDays = DateTime.DaysInMonth(dt.Year, dt.Month);
        Label dayLabel;
        int actualDay = 0;
        var weekLayOut = Owner.Find<RowLayout>("Week1");
        for (int j = 1; j <= 7; j++)
        {
            dayLabel = weekLayOut.Find("Panel" + j).Get<Label>("Label1");
            if (j < startWeekDay)
                dayLabel.Text = "";
            else
            {
                actualDay++;
                dayLabel.Text = actualDay.ToString();
            }
        }
        weekLayOut = Owner.Find<RowLayout>("Week2");
        for (int j = 1; j <= 7; j++)
        {
            dayLabel = weekLayOut.Find("Panel" + j).Get<Label>("Label1");
            actualDay++;
            if (actualDay <= maxDays)
                dayLabel.Text = actualDay.ToString();
            else
                dayLabel.Text = "";
        }
        weekLayOut = Owner.Find<RowLayout>("Week3");
        for (int j = 1; j <= 7; j++)
        {
            dayLabel = weekLayOut.Find("Panel" + j).Get<Label>("Label1");
            actualDay++;
            if (actualDay <= maxDays)
                dayLabel.Text = actualDay.ToString();
            else
                dayLabel.Text = "";
        }
        weekLayOut = Owner.Find<RowLayout>("Week4");
        for (int j = 1; j <= 7; j++)
        {
            dayLabel = weekLayOut.Find("Panel" + j).Get<Label>("Label1");
            actualDay++;
            if (actualDay <= maxDays)
                dayLabel.Text = actualDay.ToString();
            else
                dayLabel.Text = "";
        }
        weekLayOut = Owner.Find<RowLayout>("Week5");
        for (int j = 1; j <= 7; j++)
        {
            dayLabel = weekLayOut.Find("Panel" + j).Get<Label>("Label1");
            actualDay++;
            if (actualDay <= maxDays)
                dayLabel.Text = actualDay.ToString();
            else
                dayLabel.Text = "";
        }
        weekLayOut = Owner.Find<RowLayout>("Week6");
        for (int j = 1; j <= 7; j++)
        {
            dayLabel = weekLayOut.Find("Panel" + j).Get<Label>("Label1");
            actualDay++;
            if (actualDay <= maxDays)
                dayLabel.Text = actualDay.ToString();
            else
                dayLabel.Text = "";
        }
    }

    [ExportMethod]
    public void SetDate(NodeId day)
    {
        var selectedDay = InformationModel.Get(day).Get<Label>("Label1").Text;
        dt = new DateTime(dt.Year, dt.Month, int.Parse(selectedDay));
        ((IUAVariable)selectedDate).Value = dt;
    }
}
