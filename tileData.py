############################################################
#Created by Nishaan Naran
#This script takes and parses tile data from a csv file and converts it into json format
############################################################
import csv
import json
#creates empty dictionary to later output data
json_data = {}
with open ('E:\Placement\Brad Project\data.csv', mode='r') as csv_file:
    csv_reader = csv.DictReader(csv_file)
    for row in csv_reader:
        #creates key: value pair as new rows in empty dictionary
        json_data[row['Index']] = row
        #Converts the strings into ints
        json_data[row['Index']]["SpeedFactor"] = int(json_data[row['Index']]["SpeedFactor"])
        json_data[row['Index']]["FlatTileSubstitionIndex"] = int(json_data[row['Index']]["FlatTileSubstitionIndex"])
        json_data[row['Index']]["AnimationIndex"] = int(json_data[row['Index']]["AnimationIndex"])
        #Converts the "TRUE" and "FALSE" strings into bools
        json_data[row['Index']]["IsWalking_Passable"] = True if json_data[row['Index']]["IsWalking_Passable"] == "TRUE" else False
        json_data[row['Index']]["IsBoat_Passable"] = True if json_data[row['Index']]["IsBoat_Passable"] == "TRUE" else False
        json_data[row['Index']]["IsSkiff_Passable"] = True if json_data[row['Index']]["IsSkiff_Passable"] == "TRUE" else False
        json_data[row['Index']]["IsCarpet_Passable"] = True if json_data[row['Index']]["IsCarpet_Passable"] == "TRUE" else False
        json_data[row['Index']]["IsKlimable"] = True if json_data[row['Index']]["IsKlimable"] == "TRUE" else False
        json_data[row['Index']]["IsOpenable"] = True if json_data[row['Index']]["IsOpenable"] == "TRUE" else False
        json_data[row['Index']]["IsPartOfAnimation"] = True if json_data[row['Index']]["IsPartOfAnimation"] == "TRUE" else False
        json_data[row['Index']]["IsUpright"] = True if json_data[row['Index']]["IsUpright"] == "TRUE" else False
        json_data[row['Index']]["IsEnemy"] = True if json_data[row['Index']]["IsEnemy"] == "TRUE" else False
        json_data[row['Index']]["IsNPC"] = True if json_data[row['Index']]["IsNPC"] == "TRUE" else False
        json_data[row['Index']]["IsBuilding"] = True if json_data[row['Index']]["IsBuilding"] == "TRUE" else False
        json_data[row['Index']]["DontDraw"] = True if json_data[row['Index']]["DontDraw"] == "TRUE" else False
        #Deletes the row in subdict that gives index info
        del json_data[row['Index']]['Index']
        '''for row in csv_reader:
            json_data[row['Index']] = row
            some_dict =  json_data[row['Index']]
            new_dict = { key : True if value.lower() == "true" else False if value.lower() == "false" else value for key,value in some_dict.items() }
            print(new_dict)'''
print(json.dumps(json_data, indent=3))