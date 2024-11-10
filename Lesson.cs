/*namespace AC
{
	public class Lesson
	{
		public string TeacherName => $"{LastName} {FirstName} {Patronymic}";
		public string LastName { get; set; }
		public string FirstName { get; set; }
		public string Patronymic { get; set; }

		// Properties of the lesson
		public string LessonId { get; set; }
		public string Teacher { get; set; }
		public DateTime StartTime { get; set; }
		public DateTime EndTime { get; set; }
		public string Room { get; set; }
		public string Group { get; set; }
		public string Description { get; set; }
		public string QRCode { get; set; }
		public string PinCode { get; set; }
		public string TeacherUIN { get; set; }
	}

}
*/
public class FailedLoginAttempt
{
	public string UIN { get; set; }  // UIN пользователя
	public int AttemptCount { get; set; }  // Количество неудачных попыток
	public DateTime LastAttemptTime { get; set; }  // Время последней неудачной попытки
	public bool IsLocked => AttemptCount >= 5 && DateTime.Now < LastAttemptTime.AddMinutes(15); // Блокировка на 15 минут
}
