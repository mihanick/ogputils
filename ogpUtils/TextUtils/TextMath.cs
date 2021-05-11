namespace ogpUtils
{

	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;

	//Классы Teigha
	using Teigha.Runtime;
	using HostMgd.EditorInput;
	using Platform = HostMgd;
	using PlatformDb = Teigha;
	using HostMgd.ApplicationServices;
	using Teigha.DatabaseServices;
	using Teigha.Geometry;

	//Классы используются для преобразования чисел - работы с системными десятичными разделителями
	using System.Globalization;
	//Регулярные выражения используются для парсинга строчек и преобразования их в числа
	using System.Text.RegularExpressions;


	public class TextMath
	{

		Editor ed = Platform.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor;
		Database acCurDb = Platform.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Database;
		Document acCurDoc = Platform.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
		//Точность вычислений по умолчанию
		int CalculationPrecision = 3;

		//Так регистрируются команды в nanoCAD

		[CommandMethod("OGPmathSubt")]      //Вычитание
		public void mathSubst()
		{
			TwoOperandsCalculation("-");
		}
		[CommandMethod("OGPmathDiv")]       //Деление
		public void mathDiv()
		{
			TwoOperandsCalculation("/");
		}
		[CommandMethod("OGPmathMult")]      //Умножение
		public void mathMult()
		{
			MultiplyOperandsCalculation("*");
		}
		[CommandMethod("OGPmathSum")]       //Сложение
		public void mathSum()
		{
			MultiplyOperandsCalculation("+");
		}
		[CommandMethod("OGPmathPrecision")] //Точность вычислений
		public void mathPrecision()
		//Функция запрашивает в командной строке количество знаков после запятой
		//И устанавливает значение соответствующей переменной
		{
			PromptStringOptions pStrOpts = new PromptStringOptions("Укажите количество знаков после запятой: " + "[" + CalculationPrecision + "]");
			pStrOpts.AllowSpaces = true;
			PromptResult pStrRes = acCurDoc.Editor.GetString(pStrOpts);
			if (!int.TryParse(pStrRes.StringResult, out CalculationPrecision))
				ed.WriteMessage("Ошибка ввода");
		}

		//http://stackoverflow.com/questions/1354924/c-how-do-i-parse-a-string-with-a-decimal-point-to-a-double
		private double GetDoubleFromStringAndIgnoreDelimeter(string StringToParse, double defaultValue = 0)
		//Функция парсит текст регулярным выражением и выводит из него последне число в виде double
		//Без учета десятичных разделителей
		{
			double result = defaultValue;
			Regex regex = new Regex(@"^-?\d+(?:[\.\,]\d+)?");
			Match match = regex.Match(StringToParse);
			foreach (Capture capture in match.Captures)
			{
				string val = capture.Value;
				//Try parsing in the current culture
				if (!double.TryParse(val, System.Globalization.NumberStyles.Any, CultureInfo.CurrentCulture, out result) &&
				//Then try in US english
				!double.TryParse(val, System.Globalization.NumberStyles.Any, CultureInfo.GetCultureInfo("en-US"), out result) &&
				//Then in neutral language
				!double.TryParse(val, System.Globalization.NumberStyles.Any, CultureInfo.InvariantCulture, out result))
				{
					result = defaultValue;
				}
			};
			return result;
		}
		private void PlaceTextOnTheDrawing(double rVal, double TextHeightToSet = 500)
		//Функция запрашивает размещение значения rVal
		//В виде текста с высотой TextHeightToSet на чертеже
		{

			Transaction tr = Platform.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Database.TransactionManager.StartTransaction();
			try
			{
				//http://docs.autodesk.com/ACD/2010/ENU/AutoCAD%20.NET%20Developer's%20Guide/index.html?url=WS1a9193826455f5ff2566ffd511ff6f8c7ca-3b2a.htm,topicNumber=d0e36064
				// Open the Block table for read
				BlockTable acBlkTbl;
				acBlkTbl = tr.GetObject(Platform.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Database.BlockTableId,
											 OpenMode.ForRead) as BlockTable;

				// Open the Block table record Model space for write
				BlockTableRecord acBlkTblRec;
				acBlkTblRec = tr.GetObject(acBlkTbl[BlockTableRecord.ModelSpace],
												OpenMode.ForWrite) as BlockTableRecord;

				// Create a single-line text object
				DBText acText = new DBText();
				acText.SetDatabaseDefaults();

				//Разместить текст

				//http://docs.autodesk.com/ACD/2010/ENU/AutoCAD%20.NET%20Developer's%20Guide/index.html?url=WS1a9193826455f5ff2566ffd511ff6f8c7ca-422c.htm,topicNumber=d0e10522
				PromptPointResult pPtRes;
				PromptPointOptions pPtOpts = new PromptPointOptions("");

				// Prompt for the start point
				pPtOpts.Message = "\nУкажите точку вставки текста: ";
				pPtRes = Platform.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor.GetPoint(pPtOpts);

				acText.Position = pPtRes.Value;
				//Если мы находили какой-то текст в селекции, то высота возьмется с него
				acText.Height = TextHeightToSet;

				CultureInfo culture = CultureInfo.CreateSpecificCulture("ru-ru");
				string specifier = "G";
				acText.TextString = Math.Round(rVal, CalculationPrecision).ToString(specifier, culture);

				acBlkTblRec.AppendEntity(acText);
				tr.AddNewlyCreatedDBObject(acText, true);
				tr.Commit();
			}
			catch (PlatformDb.Runtime.Exception ex)
			{
				Platform.ApplicationServices.Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage(ex.Message);
				tr.Abort();
			};

		}

		private double GetTextFromObject(DBObject dbo, string ObjectClassName)
		//Функция возвращает число из объектов различного типа
		//Если типы не подходят - вернет 0
		{
			Entity ent = (Entity)dbo;

			double rslt = 0;
			//Если объект - однострочный текст
			if (ObjectClassName == "AcDbText")
			{
				DBText txt = (DBText)ent;
				//Если в тексте есть число - достать
				rslt = GetDoubleFromStringAndIgnoreDelimeter(txt.TextString);
			}
			//Объект - Мтекст
			else if (ObjectClassName == "AcDbMText")
			{
				MText txt = (MText)ent;
				rslt = GetDoubleFromStringAndIgnoreDelimeter(txt.Text);
			}

			else if (
				ObjectClassName == "AcDbRotatedDimension" ||   //Объект - Параллельный или линейный размер
				ObjectClassName == "AcDbArcDimension" ||       //Объект - Дуговой размер
				ObjectClassName == "AcDbDiametricDimension" || //Объект - Диаметральный размер
				ObjectClassName == "AcDbRadialDimension"       //Объект - Радиальный размер
			)
			{
				Dimension dim = (Dimension)ent;
				rslt = dim.Measurement;
			};

			return rslt;
		}


		private double GetDoubleSelectingObject(string strPrompt)
		//Функция предлагает выбрать один объект и возвращает из него число
		{
			double rslt = 0;
			//Выбрать объекты

			ed.WriteMessage(strPrompt);
			//Выбираем только один:
			PromptSelectionOptions pso = new PromptSelectionOptions();
			pso.SingleOnly = true;
			PromptSelectionResult sr = ed.GetSelection(pso);

			//Выбрали первый объект
			if (sr.Status == PromptStatus.OK)
			{
				Transaction tr = acCurDb.TransactionManager.StartTransaction();

				try
				{
					ObjectId[] objIds = sr.Value.GetObjectIds();
					DBObject dbo = tr.GetObject(objIds[0], OpenMode.ForRead);

					//Получим число из объекта
					rslt = GetTextFromObject(dbo, objIds[0].ObjectClass.Name);

					tr.Commit();
				}
				catch (PlatformDb.Runtime.Exception ex)
				{
					ed.WriteMessage(ex.Message);
					tr.Abort();
				};
			};
			return rslt;
		}
		public void MultiplyOperandsCalculation(string strOperation = "+")
		//Функция производит операцию над произвольным количеством текстов
		//выбранных секущей рамкой или как угодно
		//результат вычисления показывает выражением в командной строке
		//и создает текст с результатом
		{

			//Выбрать объекты
			ed.WriteMessage("Выберите объекты - текст, Мтекст, размер");
			PromptSelectionResult sr = ed.GetSelection();

			if (sr.Status == PromptStatus.OK)
			{
				Transaction tr = acCurDb.TransactionManager.StartTransaction();
				double rVal;
				if (strOperation == "*" || strOperation == "/")
					rVal = 1;
				else
					rVal = 0;
				string strVal = "Выражение: ";
				double TextHeightToSet = 500;

				try
				{
					ObjectId[] objIds = sr.Value.GetObjectIds();
					//Счетчик нужен чтобы в командной строке для последнего значения не выводить "+"
					int i = 1;
					//Пробежать по всем объектам
					foreach (ObjectId asObjId in objIds)
					{
						DBObject dbo = tr.GetObject(asObjId, OpenMode.ForRead);

						//Возьмем высоту последнего текста чтобы выставить на текст-результат
						try
						{
							DBText txt = (DBText)dbo;
							TextHeightToSet = txt.Height;
						}
						catch
						{
							//Если не найдем - будет 500
						};

						//Получим число из объекта
						double rslt = GetTextFromObject(dbo, asObjId.ObjectClass.Name);

						//произвести вычисление в соответствии с операцией
						if (rslt != 0)
						{
							if (strOperation == "+")
							{
								rVal += rslt;
							}
							else if (strOperation == "-")
							{
								rVal -= rslt;
							}
							else if (strOperation == "*")
							{
								rVal *= rslt;
							}
							else if (strOperation == "/")
							{
								rVal = rVal / rslt;
							};
							strVal += rslt;
							if (i != objIds.Length) strVal += " " + strOperation + " ";
						};

						i++;
					}
					tr.Commit();
				}
				catch (PlatformDb.Runtime.Exception ex)
				{
					ed.WriteMessage(ex.Message);
					tr.Abort();
				};

				//Вывести результат в Командную строку
				ed.WriteMessage(strVal + " = " + Math.Round(rVal, CalculationPrecision));
				//Предложить вывод текста на чертеж??

				//Сформировать текст
				PlaceTextOnTheDrawing(rVal, TextHeightToSet);

			};

			//Сохранить операцию (селекцию)??
		}

		private void TwoOperandsCalculation(string strOperation = "+")
		//Функция производит вызов селекции двух объектов 
		//Далее производит вычисления над полученными текстами
		//в соответствии с операцией strOperation
		//Выводит результат в командную строку и размещает текст на чертеже
		{

			//Задать операцию
			//Let the operation be Subtract
			double rFirst = GetDoubleSelectingObject("Выберите первый объект объект - Текст, Мтекст, Размер");
			double rSecond = GetDoubleSelectingObject("Выберите второй объект объект - Текст, Мтекст, Размер");

			Double rVal = 0;
			if (strOperation == "+")
			{
				rVal = rFirst + rSecond;
			}
			else if (strOperation == "-")
			{
				rVal = rFirst - rSecond;
			}
			else if (strOperation == "*")
			{
				rVal = rFirst * rSecond;
			}
			else if (strOperation == "/")
			{
				rVal = rFirst / rSecond;
			};

			string strVal = "Выражение: " +
						Math.Round(rFirst, CalculationPrecision) +
						" " + strOperation + " " + Math.Round(rSecond, CalculationPrecision) + " = "
						+ Math.Round(rVal, CalculationPrecision);
			//Вывести результат в Командную строку
			ed.WriteMessage(strVal);

			//Предложить вывод текста на чертеж??

			//Сформировать текст
			PlaceTextOnTheDrawing(Math.Round(rVal, CalculationPrecision));
		}


		[CommandMethod("OGPtextUnderline")]
		public void TextUnderline()
		//Функция добавляет или удаляет %%u перед выбранными однострочными текстами
		{
			//Выбрать объекты
			ed.WriteMessage("Выберите объекты - однострочные тексты");
			PromptSelectionResult sr = ed.GetSelection();

			if (sr.Status == PromptStatus.OK)
			{
				Transaction tr = acCurDb.TransactionManager.StartTransaction();
				try
				{
					ObjectId[] objIds = sr.Value.GetObjectIds();
					foreach (ObjectId asObjId in objIds)
					{
						DBObject dbo = tr.GetObject(asObjId, OpenMode.ForWrite);
						try
						{
							DBText txt = (DBText)dbo;
							string sTxt = txt.TextString;
							if (!sTxt.StartsWith("%%u"))
								sTxt = "%%u" + sTxt;
							else
								sTxt = sTxt.Substring(3);

							txt.TextString = sTxt;
						}
						catch { }
					};
					tr.Commit();
				}
				catch (PlatformDb.Runtime.Exception ex)
				{
					ed.WriteMessage(ex.Message);
					tr.Abort();
				};
			}
		}

		[CommandMethod("OGPtogglePlusMinus")]
		public void TogglePlusMinus()
		//Функция переключает '+' или '-' перед выбранными однострочными текстами
		{
			//Выбрать объекты
			ed.WriteMessage("Выберите объекты - однострочные тексты");
			PromptSelectionResult sr = ed.GetSelection();

			if (sr.Status == PromptStatus.OK)
			{
				Transaction tr = acCurDb.TransactionManager.StartTransaction();
				try
				{
					ObjectId[] objIds = sr.Value.GetObjectIds();
					foreach (ObjectId asObjId in objIds)
					{
						DBObject dbo = tr.GetObject(asObjId, OpenMode.ForWrite);
						try
						{
							DBText txt = (DBText)dbo;
							string sTxt = txt.TextString;
							if (sTxt.StartsWith("+"))
								sTxt = "-" + sTxt.Substring(1);
							else
								if (sTxt.StartsWith("-"))
								sTxt = "+" + sTxt.Substring(1);
							else
								sTxt = "-" + sTxt;
							txt.TextString = sTxt;
						}
						catch { }
					};
					tr.Commit();
				}
				catch (PlatformDb.Runtime.Exception ex)
				{
					ed.WriteMessage(ex.Message);
					tr.Abort();
				};
			}
		}



		[CommandMethod("OGPtextAlignRight")]   //Выравнивание по правому краю
		public void textAlignRight()
		{
			TextAlign(TextHorizontalMode.TextRight);
		}
		[CommandMethod("OGPtextAlignCenter")]  //Выравнивание по центру
		public void textAlignCenter()
		{
			TextAlign(TextHorizontalMode.TextMid);
		}
		[CommandMethod("OGPtextAlignLeft")]    //Выравнивание по левому краю
		public void textAlignLeft()
		{
			TextAlign(TextHorizontalMode.TextLeft);
		}

		private void TextAlign(TextHorizontalMode TypeOfAlign = TextHorizontalMode.TextLeft)
		//Функция выравнивает однострочные тексты по заданной точке
		{
			//Выбрать объекты
			ed.WriteMessage("Выберите объекты - однострочные тексты");
			PromptSelectionResult sr = ed.GetSelection();

			if (sr.Status == PromptStatus.OK)
			{

				//http://docs.autodesk.com/ACD/2010/ENU/AutoCAD%20.NET%20Developer's%20Guide/index.html?url=WS1a9193826455f5ff2566ffd511ff6f8c7ca-422c.htm,topicNumber=d0e10522
				Point3d pPtRes;
				PromptPointOptions pPtOpts = new PromptPointOptions("");

				// Prompt for the start point
				pPtOpts.Message = "\nУкажите точку выравнивания текста: ";
				pPtRes = ed.GetPoint(pPtOpts).Value;

				if (pPtRes != null)
				{
					Transaction tr = acCurDb.TransactionManager.StartTransaction();
					try
					{
						ObjectId[] objIds = sr.Value.GetObjectIds();
						foreach (ObjectId asObjId in objIds)
						{
							DBObject dbo = tr.GetObject(asObjId, OpenMode.ForWrite);
							try
							{
								//Преобразуем объекты к тексту
								DBText txt = (DBText)dbo;
								txt.HorizontalMode = TypeOfAlign;
								Point3d pAlign = new Point3d(pPtRes.X, txt.Position.Y, txt.Position.Z);
								txt.Position = pAlign;
								txt.AlignmentPoint = pAlign;
							}
							catch { /*Преобразовать не получилось*/}
						};
						tr.Commit();
					}
					catch (PlatformDb.Runtime.Exception ex)
					{
						ed.WriteMessage(ex.Message);
						tr.Abort();
					};
				};
			}

		}

		/// <summary>
		/// Поворачивает мтекст вертикально
		/// </summary>
		[CommandMethod("OgpRotateText")]
		public void RotateText()
		{
			//Выбрать объекты
			ed.WriteMessage("Выберите объекты - мтексты");
			PromptSelectionResult sr = ed.GetSelection();

			if (sr.Status == PromptStatus.OK)
			{
				Transaction tr = acCurDb.TransactionManager.StartTransaction();
				try
				{
					ObjectId[] objIds = sr.Value.GetObjectIds();
					foreach (ObjectId asObjId in objIds)
					{
						DBObject dbo = tr.GetObject(asObjId, OpenMode.ForWrite);
						try
						{
							//Преобразуем объекты к тексту
							MText txt = (MText)dbo;
							txt.Rotation = Math.PI / 2;
						}
						catch { /*Преобразовать не получилось*/}
					};
					tr.Commit();
				}
				catch (PlatformDb.Runtime.Exception ex)
				{
					ed.WriteMessage(ex.Message);
					tr.Abort();
				};
			}
		}

		[CommandMethod("OGPSumLength")]
		public void SumLength()
		//Функция выбирает из селекции линии и полилинии
		//и суммирует их длину 
		{
			//Выбрать объекты
			ed.WriteMessage("Выберите объекты - линия, полилиния");
			PromptSelectionResult sr = ed.GetSelection();

			if (sr.Status == PromptStatus.OK)
			{
				Transaction tr = acCurDb.TransactionManager.StartTransaction();
				double rVal = 0;
				try
				{
					ObjectId[] objIds = sr.Value.GetObjectIds();
					//Пробежать по всем объектам
					foreach (ObjectId asObjId in objIds)
					{
						double rslt = 0;
						try
						{
							DBObject dbo = tr.GetObject(asObjId, OpenMode.ForRead);
							//Получим длина из объекта
							Entity ent = (Entity)dbo;

							if (asObjId.ObjectClass.Name == "AcDbLine")
							{
								Line kline = (Line)ent;
								rslt = kline.Length;
							}
							else if (asObjId.ObjectClass.Name == "AcDbPolyline")
							{
								Polyline kline = (Polyline)ent;
								rslt = kline.Length;
							}
							else if (asObjId.ObjectClass.Name == "AcDbArc")
							{
								Arc kline = (Arc)ent;
								rslt = kline.Length;
							};
						}
						catch { /*Не удалось сконвертить*/};
						rVal += rslt;
					};
					tr.Commit();
				}
				catch (PlatformDb.Runtime.Exception ex)
				{
					ed.WriteMessage(ex.Message);
					tr.Abort();
				};

				//Вывести результат в Командную строку
				ed.WriteMessage("Общая длина: " + Math.Round(rVal, CalculationPrecision));
				//Предложить вывод текста на чертеж??

			};

			//Сохранить операцию (селекцию)??

		}
	}
}