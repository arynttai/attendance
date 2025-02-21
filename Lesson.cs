using System.ComponentModel.DataAnnotations.Schema;

namespace AC
{
   
[Table("lessons")]
    public class Lesson
    {
        [Column("lessonid")]
        public string LessonId { get; set; }

        [Column("teacher")]
        public string Teacher { get; set; }

        [Column("teacheruin")]
        public string TeacherUIN { get; set; }

        [Column("starttime")]
        public DateTime StartTime { get; set; }

        [Column("endtime")]
        public DateTime EndTime { get; set; }

        [Column("room")]
        public string Room { get; set; }

        [Column("group")]
        public string Group { get; set; }

        [Column("description")]
        public string Description { get; set; }

        [Column("pincode")]
        public string PinCode { get; set; }
    }


}
public class FailedLoginAttempt
{
	public string UIN { get; set; }  // UIN пользователя
	public int AttemptCount { get; set; }  // Количество неудачных попыток
	public DateTime LastAttemptTime { get; set; }  // Время последней неудачной попытки
	public bool IsLocked => AttemptCount >= 5 && DateTime.Now < LastAttemptTime.AddMinutes(15); // Блокировка на 15 минут
}
