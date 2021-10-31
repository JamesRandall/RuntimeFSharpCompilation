#r "./Model.dll"
open Model

let scriptWithUnitResult ourModel =
  printf "scriptWithUnitResult\n"
  printf $"Surname: {ourModel.Surname}\nForename: {ourModel.Forename}\n"
  
let scriptWithIntResult ourModel =
  printf "scriptWithIntResult\n"
  printf $"Surname: {ourModel.Surname}\nForename: {ourModel.Forename}\n"
  1

let add value1 value2 =
  value1 + value2
  