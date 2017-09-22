

import re
import urllib
import requests
import time
from bs4 import BeautifulSoup
import collections
from collections import Counter
from collections import defaultdict
import operator

def get_ids():
	"""Get PMC Id's from csv file """
	id_file = open('pipeline_id_list.csv', 'r')
	id_list = []
	for line in id_file:
		# rstrip() remove '\n' character
		id_list.append(line.rstrip())
	return id_list

	
def delete_words():
	"""Add to list all useless words"""
	stop_words= open('classic_word.csv', 'r')
	uselesses = open('Useless_words.csv', 'r')
	valise_list = []	
	for stop_word in stop_words:
		valise_list.append(stop_word.lower().rstrip())
	for useless in uselesses:
		valise_list.append(useless.lower().rstrip())
	valise_list = re.sub('[\;]', '', str(valise_list))	
	print(valise_list)
	return valise_list
		
		
def get_article(pmcid):
	""" Download PMC's Article"""
	# view-source:https://eutils.ncbi.nlm.nih.gov/entrez/eutils/efetch.fcgi?db=pmc&format=xml&id=PMC5000013
	req = 'https://eutils.ncbi.nlm.nih.gov/entrez/eutils/efetch.fcgi?db=pmc&format=xml&id=PMC' + str(pmcid)
	r  = urllib.request.Request(req)
	data = urllib.request.urlopen(r).read()
	soup = BeautifulSoup(data, 'html.parser')
	article = soup.find_all('body')
	return article
	

def clean_html(article):
	""" Clean HTML tags"""
	clean_article = re.sub('<.*?>', '', str(article))
	clean_article = re.sub('[()\'\[\]\.\,\/\%\"\&\_\=\;\:]', '', str(clean_article))
	clean_article = re.sub('[0-9]', '', str(clean_article))
	return clean_article
	
	
def split_words(clean_article):
	"""split article word by word"""
	split_article= clean_article.split()
	return split_article


def clean_words(split_article, useless):
	"""Delete article's word which are in the useless words"""
	clean_text = []
	for word in split_article:
		if word.lower() not in useless:
			clean_text.append(word.lower())			
	return clean_text		

	
def count_words(words):
	"""Count each word ocurency in the article """
	count= Counter(words)
	return count
	
	
def dico_rank(dico):
	"""sort Dictionary"""
	dico_trie = sorted(dico.items(), reverse=True, key=operator.itemgetter(1))
	return dico_trie
	
	
def write_csv(dico):
	"""Write the csv file"""
	file_towrite = open('./list_word_pipeline.csv', 'w')
	for word in dico:
		if word[1]>4:
			file_towrite.write('{}\t{}\n'.format(word[0], word[1]))
	file_towrite.close()	
	
	
if __name__ == '__main__':
	begin = time.time()	
	# IDs to parse
	id_list = get_ids()
	final_dict = {}
	useless_words = delete_words()
	for pmcid in id_list:
		article = get_article(pmcid)
		clean_article = clean_html(article)
		split_article = split_words(clean_article)
		words = clean_words(split_article, useless_words)
		dictio = count_words(words)
		print(dictio)
		final_dict= Counter(final_dict) + Counter(dictio)	
	ranked_dico= dico_rank(final_dict)
	write_csv(ranked_dico)
	print("ok")
	end = time.time()-begin
	print(end)

		
		
		

