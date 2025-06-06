using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.CMS.Domain.Enums
{
    public enum PageStatus
    {
        Draft = 0,
        Published = 1,
        Archived = 2,
        Scheduled = 3
    }

    public enum ComponentType
    {
        Text = 0,
        Image = 1,
        Button = 2,
        Container = 3,
        Grid = 4,
        Card = 5,
        List = 6,
        Form = 7,
        Video = 8,
        Map = 9,
        Gallery = 10,
        Slider = 11,
        Navigation = 12,
        Footer = 13,
        Header = 14,
        Sidebar = 15
    }
}
