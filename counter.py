import os
from time import sleep

def count_lines_in_files(directory):
    total_lines = 0
    extensions = ('.cs', '.vert', '.frag')

    for root, dirs, files in os.walk(directory):
        # Skip directories named 'bin'
        dirs[:] = [d for d in dirs if d != 'bin']
        
        for file in files:
            if file.endswith(extensions):
                file_path = os.path.join(root, file)
                with open(file_path, 'r', encoding='utf-8', errors='ignore') as f:
                    lines = f.readlines()
                    total_lines += len(lines)

    return total_lines

directory = 'C:\\Users\\timur\\RiderProjects\\Voxel-Game\\'
total_lines = count_lines_in_files(directory)
print(f'Total lines in .cs, .vert, and .frag files: {total_lines}')

sleep(100000)
