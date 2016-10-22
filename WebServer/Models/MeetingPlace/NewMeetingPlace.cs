﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebServer.Models.MeetingPlace
{
    //提供给浏览器进行添加新会场的信息类
    public class NewMeetingPlace
    {
        public string meetingPlaceName { set; get; }
        public string meetingPlaceType { set; get; }
        public int meetingPlaceCapacity { set; get; }
    }
}